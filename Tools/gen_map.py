# -*- coding: utf-8 -*-
"""大地图（三消关卡地图）重绘——四季地块 + 关卡按钮 + 云。

为什么不能用通用提示词：
  通用模板（"a casual game level map element: ..."）会让模型画成「一个立体的
  地图装饰物」——实测把四季地块画成了奶油蛋糕塔，路径消失、地形色丢失，
  完全无法铺成大地图。地图需要的是**俯视平面地形**这一特定结构。

结构要求（本脚本的提示词核心）：
  - top-down / slightly tilted orthographic，不是斜视立体
  - 大面积纯色地形块（四季各自的主色）
  - 一条蜿蜒的浅色路径贯穿全图（玩家沿路径走关卡）
  - 极小的建筑点缀，不抢地形

尺寸：原图 2200x2400 超出 API 上限(2048²)，故按比例出 1536x1024/2048² 再放大。

用法: python Tools/gen_map.py [--apply] [--only map_c1.png,...]
"""
import argparse
import os
import subprocess
import sys

from PIL import Image

SKILL = os.path.expanduser("~/.claude/skills/gpt-image-2/gpt_image_2.py")
HERE = os.path.dirname(os.path.abspath(__file__))
ART = os.path.join(HERE, "..", "Assets", "Project Files", "Game", "Art", "Map")
OUT = "D:/claudeWorkbase/_redraw/Map"

BASE = (
    "A top-down level-select MAP TILE for a casual mobile game, seen from a slightly "
    "tilted overhead angle (like a map viewed from above, NOT a 3D object photo). "
    "This is a TERRAIN PIECE that tiles with other pieces to form one large world map. "
    "STRUCTURE: one large flat region of solid terrain colour filling most of the canvas, "
    "with a single winding cream-coloured walking PATH running through it, and a few "
    "TINY simple buildings or trees as distant decoration. "
    "The terrain colour must dominate - it is the most important element. "
    "The path must be clearly readable as a route. "
    "Hand-painted casual game illustration, soft matte finish, gentle shading. "
    "Do NOT draw a large central building, castle, cake or landmark - that would break "
    "the map. Decoration stays small and at the edges. "
    "Fully transparent background outside the terrain shape. No text, no UI, no buttons."
)

# 非地形类资源（按钮/云）用这个，不含 terrain 语义
BASE_NON_TERRAIN = (
    "A single isolated UI element for a casual mobile game world map. "
    "Hand-painted casual game illustration, soft matte finish, warm cream and "
    "butter-ivory palette with a thin champagne-gold rim. "
    "Fully transparent background. No terrain, no landscape, no ground, no text."
)

NON_TERRAIN = {"map_level.png", "cloud.png"}

ITEMS = {
    "map_c1.png": ("SPRING GRASSLAND", "fresh bright green grass with lighter yellow-green "
                   "patches, small round trees and tiny white flowers"),
    "map_c2.png": ("AUTUMN HARVEST", "warm golden-yellow fields and ochre dunes with "
                   "cream paths, small autumn trees with yellow leaves"),
    "map_c3.png": ("WINTER SNOWFIELD", "soft white snow with pale pink-blue shadows, "
                   "bare trees with snow caps, small snow-covered huts"),
    "map_c4.png": ("SPRING BLOSSOM", "fresh green meadow with pink cherry-blossom trees "
                   "and warm cream ground patches"),
    "map_c0.png": ("STARTING PLAIN", "bright green meadow with cream paths and tiny "
                   "gingerbread-style cottages"),
    "map_level.png": ("LEVEL BUTTON", "a soft cream circular plate with a thin warm-gold "
                      "rim and a slightly translucent face, EMPTY in the centre so a level "
                      "number can be drawn on top later. Flat front view, circular, no text, "
                      "no number, no icon"),
    "cloud.png": ("CLOUD", "a soft fluffy cartoon cloud, cream-white with warm underside "
                  "shading, side view, isolated"),
}


def api_size(w, h):
    """按比例选 API 支持尺寸（不放大到超过原图）。"""
    for s in [(2048, 2048), (1536, 1024), (1024, 1536)]:
        if abs(s[0] / s[1] - w / float(h)) <= 0.25:
            return "%dx%d" % s
    return "2048x2048"


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    ap = argparse.ArgumentParser()
    ap.add_argument("--apply", action="store_true")
    ap.add_argument("--only")
    a = ap.parse_args()

    names = sorted(ITEMS)
    if a.only:
        names = [x.strip() for x in a.only.split(",")]
    os.makedirs(OUT, exist_ok=True)

    print("大地图重绘 · %d 张\n" % len(names))
    plan = []
    for n in names:
        title, desc = ITEMS[n]
        p = os.path.join(ART, n)
        size = Image.open(p).size if os.path.exists(p) else (0, 0)
        if n in NON_TERRAIN:
            prompt = "%s: %s. %s" % (title, desc, BASE_NON_TERRAIN)
        else:
            prompt = "%s. Terrain: %s. %s" % (title, desc, BASE)
        plan.append((n, size, prompt))
        print("  %-18s %-14s %s" % (n, "%dx%d" % size, title))

    if not a.apply:
        print("\n[dry-run] 加 --apply 执行。预估 $%.1f" % (len(plan) * 0.19))
        return

    ok = fail = 0
    import time as _t
    for n, size, prompt in plan:
        cmd = [sys.executable, SKILL, "t2i", "--style", "bakery-painterly",
               "-p", prompt, "--name", os.path.splitext(n)[0],
               "-s", api_size(*size), "--bg", "transparent", "-o", OUT]
        good = False
        for att in range(4):
            r = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8",
                               errors="replace", timeout=900)
            if r.returncode == 0:
                good = True
                break
            msg = ((r.stdout or "") + (r.stderr or ""))[-200:]
            if not any(k in msg.lower() for k in ("too many concurrent", "rate limit",
                                                  "precharge", "timeout", "502", "503")):
                print("  ❌ %-18s %s" % (n, msg[:120]))
                break
            _t.sleep(min(2 ** att * 5, 30))
        ok += good
        fail += (not good)
        if good:
            # 尺寸对齐：等比放大到原图尺寸
            p2 = os.path.join(OUT, n)
            im = Image.open(p2).convert("RGBA")
            if im.size != size:
                im = im.resize(size, Image.LANCZOS)
                im.save(p2)
            print("  ✅ %-18s %dx%d → %dx%d" % (n, im.width, im.height, size[0], size[1]))

    print("\n完成：成功 %d，失败 %d" % (ok, fail))


if __name__ == "__main__":
    main()
