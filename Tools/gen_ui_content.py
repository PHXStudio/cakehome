# -*- coding: utf-8 -*-
"""UI 内容物重绘（只跑 ui_classify 判定为「可重绘」的 25 张）。

几何符号 / 九宫格 / 纯底渐变 一律不碰——那些走 reskin_soften.py 调色。

用法: python Tools/gen_ui_content.py [--apply] [--list]
"""
import argparse
import concurrent.futures as cf
import os
import subprocess
import sys

from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from ui_classify import classify, GROUPS  # noqa: E402

SKILL = os.path.expanduser("~/.claude/skills/gpt-image-2/gpt_image_2.py")
HERE = os.path.dirname(os.path.abspath(__file__))
ART = os.path.join(HERE, "..", "Assets", "Project Files", "Game", "Art")
OUT = "D:/claudeWorkbase/_redraw"

CUTOUT = ("Isolated cutout on a completely transparent background: absolutely NO ground, "
          "NO floor, NO backdrop, NO cast shadow on background, NO glow, NO vignette. "
          "Centered, hand-painted casual game illustration with rich material texture, "
          "warm key light from upper front, soft ambient occlusion. Warm cream, caramel "
          "and soft pastel palette. No text, no letters.")

# 逐个资源的手写描述（文件名推导不够准，这些要精确）
DESC = {
    "coin": "a single shiny gold coin with an embossed star emblem",
    "heart": "a plump red heart icon with a soft highlight",
    "home icon": "a cozy little house icon with a warm roof",
    "ui_icon_coin": "a stack of gold coins with a star emblem",
    "ui_icon_coin_x2": "a small pile of two gold coins",
    "ui_icon_coin_x3": "a stack of three gold coins",
    "ui_icon_coin_x4": "a taller stack of four gold coins",
    "ui_icon_coin_x5": "a large heap of five gold coins",
    "ui_icon_energy": "a glowing blue lightning bolt energy symbol",
    "ui_icon_energy_x3": "three glowing blue lightning bolts",
    "ui_icon_energy_x4": "four glowing blue lightning bolts arranged together",
    "ui_icon_energy_x5": "five glowing blue lightning bolts in a cluster",
    "ui_icon_gameboard": "a small match-3 game board with colourful tiles",
    "ui_icon_gem": "a single sparkling purple gemstone, faceted",
    "ui_icon_gem_x2": "two sparkling purple gemstones",
    "ui_icon_gem_x3": "three sparkling purple gemstones",
    "ui_icon_gem_x4": "four sparkling purple gemstones",
    "ui_icon_gem_x5": "five sparkling purple gemstones in a pile",
    "ui_icon_map": "a rolled parchment treasure map with a red pin",
    "ui_icon_star": "a bright yellow five-pointed star",
    "ui_icon_star_empty": "a hollow outline of a five-pointed star, muted grey-cream",
    "ui_icon_store": "a small bakery shop storefront icon with a striped awning",
    "ic_coins_pack_1": "a small pouch of gold coins, the starter coin pack",
    "ic_coins_pack_2": "a wooden crate overflowing with gold coins",
    "ic_coins_pack_3": "a treasure chest bursting with a huge amount of gold coins",
}


def api_size(w, h):
    sizes = [(1024, 1024), (1024, 1536), (1536, 1024), (2048, 2048)]
    ar = w / float(h)

    def ok(s):
        return abs(s[0] / s[1] - ar) <= 0.02

    c = [s for s in sizes if ok(s)]
    if c:
        e = [s for s in c if s[0] * s[1] >= w * h]
        return "%dx%d" % min(e or c, key=lambda s: s[0] * s[1])
    return "%dx%d" % min(sizes, key=lambda s: abs(s[0] / s[1] - ar))


def build():
    items = []
    for grp in GROUPS:
        d = os.path.join(ART, grp)
        if not os.path.isdir(d):
            continue
        for f in sorted(os.listdir(d)):
            if not f.endswith(".png"):
                continue
            kind, verdict, _ = classify(f, os.path.join(d, f))
            if verdict != "可重绘":
                continue
            key = os.path.splitext(f)[0]
            desc = DESC.get(key, key.replace("_", " "))
            size = Image.open(os.path.join(d, f)).size
            items.append({
                "group": grp, "name": f, "size": size, "desc": desc,
                "out": os.path.join(OUT, grp),
                "src": os.path.join(d, f),
            })
    return items


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    ap = argparse.ArgumentParser()
    ap.add_argument("--apply", action="store_true")
    ap.add_argument("--jobs", type=int, default=3)
    a = ap.parse_args()

    items = build()
    print("UI 内容物重绘 · %d 张（几何/九宫格已排除）\n" % len(items))
    for i in items:
        print("  %-42s %-12s %s" % (i["group"] + "/" + i["name"],
                                    "%dx%d" % i["size"], i["desc"][:44]))
    if not a.apply:
        print("\n[dry-run] 加 --apply 执行。预估 $%.1f" % (len(items) * 0.19))
        return

    for d in {i["out"] for i in items}:
        os.makedirs(d, exist_ok=True)

    def run(i):
        prompt = "A mobile game UI icon showing %s. %s" % (i["desc"], CUTOUT)
        cmd = [sys.executable, SKILL, "t2i", "--style", "bakery-painterly", "-p", prompt,
               "--name", os.path.splitext(i["name"])[0], "-s", api_size(*i["size"]),
               "--bg", "transparent", "-o", i["out"]]
        import time as _t
        for attempt in range(4):
            r = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8",
                               errors="replace", timeout=900)
            if r.returncode == 0:
                return (i, True, "")
            msg = ((r.stdout or "") + (r.stderr or ""))[-200:]
            low = msg.lower()
            if not any(k in low for k in ("too many concurrent", "rate limit",
                                          "precharge", "timeout", "502", "503")):
                return (i, False, msg)
            _t.sleep(min(2 ** attempt * 5, 30))
        return (i, False, msg)

    ok = fail = 0
    errs = []
    with cf.ThreadPoolExecutor(max_workers=a.jobs) as ex:
        for n, (i, good, err) in enumerate(ex.map(run, items), 1):
            ok += good
            if not good:
                fail += 1
                errs.append((i["name"], err))
            print("[%d/%d] %s %s" % (n, len(items), "✅" if good else "❌", i["name"]))
            if not good:
                print("     ", err[:140])

    # 尺寸对齐 + 透明校验
    print("\n尺寸对齐 + 透明校验…")
    for i in items:
        p = os.path.join(i["out"], i["name"])
        if not os.path.exists(p):
            continue
        im = Image.open(p).convert("RGBA")
        w, h = i["size"]
        if im.size != (w, h):
            src_ar, dst_ar = im.width / im.height, w / float(h)
            if abs(src_ar - dst_ar) < 1e-3:
                im = im.resize((w, h), Image.LANCZOS)
            else:
                s = min(w / im.width, h / im.height)
                nw, nh = max(1, round(im.width * s)), max(1, round(im.height * s))
                cv = Image.new("RGBA", (w, h), (0, 0, 0, 0))
                cv.paste(im.resize((nw, nh), Image.LANCZOS),
                         ((w - nw) // 2, (h - nh) // 2))
                im = cv
            im.save(p)
        hh = im.getchannel("A").histogram()
        print("   %-42s %dx%d 透明%.1f%%" % (i["name"], w, h, hh[0] / sum(hh) * 100))

    print("\n完成：成功 %d，失败 %d" % (ok, fail))
    for n, e in errs[:10]:
        print("   ❌ %-30s %s" % (n, e[:110]))


if __name__ == "__main__":
    main()
