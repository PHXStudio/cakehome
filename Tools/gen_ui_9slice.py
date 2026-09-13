# -*- coding: utf-8 -*-
"""九宫格 UI 基件重绘 + 自动重算 spriteBorder。

与通用重绘的区别（这是通用提示词做不了九宫格的原因）：
  九宫格要求「四角有装饰、四边均匀、中心纯色可拉伸」的结构。
  通用提示词会产出「一个完整的装饰按钮」，中心有内容 → 拉伸后必然撕裂。

本脚本的提示词显式约束这个结构：
  - 中心区域必须是**完全均匀的纯色**，无任何图案/渐变/高光
  - 四角是圆角与描边，四边是均匀的纯色带
  - 只有边缘一圈有 3D 厚度感

生成后用 analyze_border() 自动算出新的 spriteBorder 写回 .meta。

用法:
  python Tools/gen_ui_9slice.py --test btn_gray.png      # 试 1 张（不写 meta）
  python Tools/gen_ui_9slice.py --apply                  # 全部跑 + 写 border
"""
import argparse
import json
import os
import re
import subprocess
import sys

import numpy as np
from PIL import Image

SKILL = os.path.expanduser("~/.claude/skills/gpt-image-2/gpt_image_2.py")
HERE = os.path.dirname(os.path.abspath(__file__))
ART = os.path.join(HERE, "..", "Assets", "Project Files", "Game", "Art")
OUT = "D:/claudeWorkbase/_redraw_9slice"

# 九宫格专用提示词：结构约束是第一位的
NINESLICE = (
    "A flat UI button base plate for a mobile game, designed as a 9-slice sprite. "
    "STRUCTURE REQUIREMENTS (critical): "
    "1) The CENTER area must be one completely UNIFORM SOLID COLOR with NO pattern, "
    "NO gradient, NO highlight, NO ornament, NO texture whatsoever - it must be safe "
    "to stretch horizontally and vertically. "
    "2) All decoration, rounding, bevel and 3D thickness live ONLY in a narrow border "
    "band around the outer edge. "
    "3) The four edges are uniform straight bands of the same solid colour. "
    "4) The four corners are identical simple rounded corners. "
    "Style: clean, simple, flat matte finish with a subtle darker bottom lip for a soft "
    "3D feel. Colours: {color}. "
    "Isolated object on a fully transparent background, no shadow outside the shape, "
    "no text, no letters, no icon, no symbol. Just the empty plate."
)

# 每个资源的颜色（从原图取主色，保持一致以免破坏 UI 配色体系）
COLORS = {
    "btn_gray.png": "medium warm grey",
    "btn_green.png": "fresh mint green",
    "btn_orange.png": "warm orange",
    "btn_purple.png": "soft purple",
    "ui_button_blue.png": "clear blue",
    "ui_button_gray.png": "medium grey",
    "ui_button_green.png": "bright lime green",
    "ui_button_orange.png": "warm orange",
    "ui_button_white.png": "off-white cream",
    "panel.png": "warm cream beige",
    "panel_blue.png": "soft sky blue",
    "panel_dark.png": "dark warm brown",
    "panel_red.png": "soft warm red",
    "ui_panel_white.png": "pure white",
    "ui_panel_bevel.png": "warm cream beige",
    "ui_panel_inner_white.png": "pure white",
    "ui_fill_bar.png": "warm yellow and cream",
    "ui_header.png": "warm cream beige",
    "grad_shine_square.png": "soft transparent white gradient",
    "badge_orange.png": "warm orange",
    "header_orange.png": "warm orange",
    "item_background.png": "warm cream beige",
    "item_frame.png": "warm gold and cream",
    "panel_blue_store.png": "soft blue",
    "panel_purple_store.png": "soft purple",
    "panel_red_white.png": "warm red and white",
    "panel_yellow_white.png": "warm yellow and white",
}


def dominant_color(p):
    """取图片主体（中心 50%）的主色，用于提示词。"""
    im = Image.open(p).convert("RGBA")
    a = np.asarray(im)
    h, w, _ = a.shape
    c = a[h // 4:h * 3 // 4, w // 4:w * 3 // 4]
    solid = c[..., 3] > 200
    if not solid.any():
        return None
    return c[..., :3][solid].mean(axis=0)


def analyze_border(p, tol=6.0):
    """自动推算 spriteBorder：找中心可拉伸区的边界。

    从中心向外扩，直到该行列的像素变化超过 tol（说明进入装饰区）。
    返回 (left, bottom, right, top) —— 对应 Unity 的 L,B,R,T。
    """
    im = Image.open(p).convert("RGBA")
    a = np.asarray(im).astype(float)
    h, w, _ = a.shape
    rgb = a[..., :3]
    al = a[..., 3]

    cy, cx = h // 2, w // 2
    # 从中心向各方向走，遇到明显变化就停
    def walk(dy, dx, limit):
        base = rgb[cy, cx]
        base_a = al[cy, cx]
        n = 0
        y, x = cy, cx
        while n < limit:
            y += dy
            x += dx
            if not (0 <= y < h and 0 <= x < w):
                break
            if abs(al[y, x] - base_a) > 40:
                break
            if np.abs(rgb[y, x] - base).mean() > tol:
                break
            n += 1
        return n

    left = walk(0, -1, cx)
    right = walk(0, 1, w - 1 - cx)
    top = walk(-1, 0, cy)
    bottom = walk(1, 0, h - 1 - cy)
    # border = 从边缘到可拉伸区的距离
    return (left, bottom, right, top)


def write_border(png, border):
    meta = png + ".meta"
    if not os.path.exists(meta):
        return False
    t = open(meta, encoding="utf-8", errors="replace").read()
    l, b, r, tp = border
    new = re.sub(
        r"spriteBorder:\s*\{x:[^}]*\}",
        "spriteBorder: {x: %d, y: %d, z: %d, w: %d}" % (l, b, r, tp), t)
    if new == t:
        return False
    open(meta, "w", encoding="utf-8", newline="\n").write(new)
    return True


def find_all():
    """扫描所有带 spriteBorder 的 UI 资源。"""
    sys.path.insert(0, HERE)
    from ui_classify import sprite_border
    items = []
    for grp in ["UI/Common", "UI/Store"]:
        d = os.path.join(ART, grp)
        for f in sorted(os.listdir(d)):
            if not f.endswith(".png"):
                continue
            p = os.path.join(d, f)
            if sprite_border(p):
                items.append((grp, f, p))
    return items


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    ap = argparse.ArgumentParser()
    ap.add_argument("--apply", action="store_true")
    ap.add_argument("--test", help="只跑指定文件（对比原图 border 与新算 border）")
    ap.add_argument("--jobs", type=int, default=2)
    ap.add_argument("--write-meta", action="store_true",
                    help="把新算的 border 写回 .meta（默认只打印）")
    a = ap.parse_args()

    items = find_all()
    if a.test:
        items = [i for i in items if i[1] == a.test] or \
                [("UI/Common", a.test, os.path.join(ART, "UI/Common", a.test))]
    print("九宫格资源 %d 张\n" % len(items))

    os.makedirs(OUT, exist_ok=True)
    import time as _t
    for grp, f, src in items:
        color = COLORS.get(f)
        if not color:
            c = dominant_color(src)
            color = ("rgb(%d,%d,%d)" % tuple(round(x) for x in c)) if c is not None \
                else "warm cream"
        old_b = None
        sys.path.insert(0, HERE)
        from ui_classify import sprite_border
        old_b = sprite_border(src)
        size = Image.open(src).size

        prompt = NINESLICE.format(color=color)
        name = os.path.splitext(f)[0] + "_9s"
        dst_dir = os.path.join(OUT, grp)
        os.makedirs(dst_dir, exist_ok=True)
        cmd = [sys.executable, SKILL, "t2i", "--style", "bakery-painterly",
               "-p", prompt, "--name", name, "-s", "%dx%d" % size,
               "--bg", "transparent", "-o", dst_dir]
        if not a.apply:
            print("  %-28s %-12s 原border=%s  color=%s"
                  % (f, "%dx%d" % size, old_b, color))
            continue
        r = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8",
                           errors="replace", timeout=900)
        if r.returncode != 0:
            print("  ❌ %-28s %s" % (f, ((r.stdout or "") + (r.stderr or ""))[-120:]))
            continue
        prod = os.path.join(dst_dir, name + ".png")
        im = Image.open(prod).convert("RGBA")
        if im.size != size:
            im = im.resize(size, Image.LANCZOS)
            im.save(prod)
        nb = analyze_border(prod)
        same = "（与原一致）" if old_b and tuple(round(x) for x in old_b) == nb else ""
        print("  ✅ %-28s 原border=%-24s 新算border=%-24s %s"
              % (f, str(old_b), str(nb), same))
        if a.write_meta:
            if write_border(src, nb):
                print("      → 已写回 .meta")
        _t.sleep(1)

    if not a.apply:
        print("\n[dry-run] 加 --apply 执行")


if __name__ == "__main__":
    main()
