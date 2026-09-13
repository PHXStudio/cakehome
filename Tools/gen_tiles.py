# -*- coding: utf-8 -*-
"""三消棋子重绘（Gameplay/Tiles，15 张图案 + 1 张底）。

用途核实（运行时实测）：
  棋盘每格 = Back(tile_background) + Icon(tile_cX_tY) 两层叠加
  tile_cX_tY 的 c=主题集合、t=等级；玩家靠图案判断能否消除与等级

之前失败的教训：
  用「棋盘格底图」的提示词 → 15 张全变成同款空框，玩家无法辨认 → 游戏不可玩

本脚本的正确做法：
  逐级描述**具体物品名**，强调「图案是主体、要能被区分」
  tile_background 则相反：它是空格底，中心必须留白

用法: python Tools/gen_tiles.py [--apply] [--theme c1|c2|c3|bg] [--limit N]
"""
import argparse
import os
import subprocess
import sys

from PIL import Image

SKILL = os.path.expanduser("~/.claude/skills/gpt-image-2/gpt_image_2.py")
HERE = os.path.dirname(os.path.abspath(__file__))
ART = os.path.join(HERE, "..", "Assets", "Project Files", "Game", "Art",
                   "Gameplay", "Tiles")
OUT = "D:/claudeWorkbase/_redraw/Gameplay/Tiles"

# 三套主题，每套 5 级（从原图逐张比对确认的物品）
THEMES = {
    "c1": ("SWEET BAKERY", [
        "a round golden butter bun with a soft domed top",
        "a strawberry tart with sliced strawberries in a pastry shell",
        "a blueberry muffin in a pleated paper liner",
        "a square slice of layered tiramisu dusted with cocoa",
        "a round chocolate-chip cookie with visible chips",
    ]),
    "c2": ("COFFEE HOUSE", [
        "three roasted coffee beans clustered together",
        "an orange ceramic cup filled with black coffee",
        "a light blue cup of cappuccino with latte art",
        "a tall glass of iced coffee with ice cubes and a mint leaf",
        "a light blue cup with a sleeping cat face drawn in the foam",
    ]),
    "c3": ("CANDY SHOP", [
        "a pink-and-white swirl lollipop on a stick",
        "a single round pastel purple gumdrop candy",
        "a wrapped light blue candy with twisted wrapper ends",
        "a glass jar packed with colourful round candy balls",
        "a dark cola glass bottle with a red label",
    ]),
}

# 棋子：图案是主体，要清晰可辨、单件居中、无外框
PIECE = (
    "A match-3 game tile ICON showing {item}. "
    "The ITEM FILLS the tile and is the visible subject - this is a game piece, "
    "not a frame and not an empty background. "
    "Cute casual game illustration, thick volumetric form, rich material texture, "
    "warm key light from upper front, soft ambient occlusion, gentle contact shadow. "
    "Warm cream and caramel palette with natural colour for the food itself. "
    "Isolated on a fully transparent background. "
    "No border, no frame, no rounded square plate, no text, no background scenery."
)

# 空格底：中心必须留白（上面要叠棋子图案）
BG = (
    "An EMPTY match-3 board cell background plate: a soft rounded-square surface in "
    "warm cream and butter-ivory, with a thin champagne-gold rim and a subtle inner "
    "bevel. The CENTRE must be completely flat and uniform solid colour with no "
    "pattern, no icon, no ornament - because a game piece icon is drawn on top of it. "
    "Top-down orthographic view, soft matte finish. "
    "Isolated on a fully transparent background. No text, no character, no food."
)


def api_size(w, h):
    sizes = [(512, 512), (1024, 1024)]
    # API 最小像素预算限制，必须 >=1024
    return "1024x1024"


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    ap = argparse.ArgumentParser()
    ap.add_argument("--apply", action="store_true")
    ap.add_argument("--theme", help="c1 / c2 / c3 / bg")
    ap.add_argument("--limit", type=int)
    a = ap.parse_args()

    plan = []
    if a.theme in (None, "bg"):
        plan.append(("tile_background.png", BG))
    for key, (title, items) in THEMES.items():
        if a.theme and a.theme != key:
            continue
        for i, item in enumerate(items, 1):
            plan.append(("tile_%s_t%d.png" % (key, i),
                         PIECE.format(item=item)))
    if a.limit:
        plan = plan[:a.limit]

    print("三消棋子重绘 · %d 张\n" % len(plan))
    for n, prompt in plan:
        p = os.path.join(ART, n)
        size = Image.open(p).size if os.path.exists(p) else (0, 0)
        print("  %-22s %-12s %s" % (n, "%dx%d" % size, prompt[:56]))

    if not a.apply:
        print("\n[dry-run] 加 --apply 执行。预估 $%.1f" % (len(plan) * 0.19))
        return

    os.makedirs(OUT, exist_ok=True)
    import time as _t
    ok = fail = 0
    for n, prompt in plan:
        src = os.path.join(ART, n)
        size = Image.open(src).size if os.path.exists(src) else (512, 512)
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
                print("  ❌ %-22s %s" % (n, msg[:110]))
                break
            _t.sleep(min(2 ** att * 5, 30))
        ok += good
        fail += (not good)
        if good:
            p2 = os.path.join(OUT, n)
            im = Image.open(p2).convert("RGBA")
            if im.size != size:
                im = im.resize(size, Image.LANCZOS)
                im.save(p2)
            print("  ✅ %-22s %dx%d" % (n, size[0], size[1]))

    print("\n完成：成功 %d，失败 %d" % (ok, fail))


if __name__ == "__main__":
    main()
