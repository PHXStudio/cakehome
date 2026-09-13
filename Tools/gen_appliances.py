# -*- coding: utf-8 -*-
"""kettle / mixer 器具链重绘（独立模板，不套甜点模板）。

问题背景：Gameplay/Pieces 的甜点模板强制 "dessert form / fondant material"
并加 "do not upgrade into a fancier dessert" —— 对咖啡机、厨师机这类**电器**
是错的，会把高阶电器压回基础形态（实测 9 张全画成茶壶/蛋抽）。

本脚本用独立模板：强调「厨房器具沿科技演进」，越高级越现代越电动。

用法: python Tools/gen_appliances.py [--apply] [--only kettle_9,mixer_9]
"""
import argparse
import os
import subprocess
import sys

from PIL import Image

SKILL = os.path.expanduser("~/.claude/skills/gpt-image-2/gpt_image_2.py")
ART = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                   "..", "Assets", "Project Files", "Game", "Art")
SRC = os.path.join(ART, "Gameplay", "Pieces")
OUT = "D:/claudeWorkbase/_redraw/Gameplay/Pieces"

CHAINS = {
    "kettle": [
        "a simple terracotta clay teapot with a round body and a small knob lid",
        "a glossy bright red ceramic teapot with a curved spout",
        "a tall slender red ceramic coffee pot with a long thin spout",
        "a polished stainless steel stove-top whistling kettle with a black handle",
        "a brushed stainless steel gooseneck pour-over kettle",
        "a clear glass drip coffee carafe on a black plastic base, dark coffee inside",
        "a modern electric kettle in red and yellow plastic with a water level window "
        "and a power switch, ELECTRIC APPLIANCE",
        "a beige semi-automatic espresso machine with a portafilter, a steam wand and "
        "one cup under the spout, ELECTRIC APPLIANCE",
        "a premium beige espresso machine with TWO portafilters, two cups, a steam wand "
        "and a round pressure gauge on the front, ELECTRIC APPLIANCE",
    ],
    "mixer": [
        "a mint-green handled measuring scoop filled with pale batter",
        "a mint-green handled wire whisk",
        "an empty cream ceramic mixing bowl",
        "a cream ceramic mixing bowl with a mint-green whisk resting inside",
        "a mint-green handheld electric stick blender with a button, ELECTRIC APPLIANCE",
        "a mint-green handheld electric hand mixer with two beaters, ELECTRIC APPLIANCE",
        "a mint-green electric hand mixer resting upright in a cream mixing bowl, "
        "ELECTRIC APPLIANCE",
        "a mint-green stand mixer with a stainless steel bowl and a dough hook, "
        "ELECTRIC APPLIANCE",
        "a mint-green stand mixer with a stainless steel bowl and a wire whisk "
        "attachment, ELECTRIC APPLIANCE",
    ],
}

TEMPLATE = (
    "A kitchen appliance, part of a step-by-step UPGRADE CHAIN in a cooking game: "
    "{item}. "
    "This is NOT a dessert or a food item - it is a piece of kitchen equipment. "
    "Render its real-world form faithfully: correct silhouette for a {short}, "
    "believable metal / glass / plastic / ceramic materials, visible mechanical "
    "detail (buttons, handles, dials, cords) where applicable. "
    "The higher the tier, the more modern and more electric the appliance looks. "
    "Hand-painted casual game illustration with rich material texture and volume, "
    "warm key light from upper front, soft ambient occlusion, warm cream and mint "
    "palette with metal accents. "
    "Slightly angled front view, single object centered. "
    "Isolated cutout on a completely transparent background: NO ground, NO floor, "
    "NO backdrop, NO cast shadow on background, NO glow. No text, no letters."
)


def short_name(item):
    """从描述里抽一个简短名词，供模型定位物件类型。"""
    for key in ("espresso machine", "coffee carafe", "coffee pot", "electric kettle",
                "whistling kettle", "pour-over kettle", "stand mixer", "hand mixer",
                "stick blender", "mixing bowl", "measuring scoop", "wire whisk",
                "teapot", "whisk"):
        if key in item:
            return key
    return "kitchen appliance"


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    ap = argparse.ArgumentParser()
    ap.add_argument("--apply", action="store_true")
    ap.add_argument("--only", help="只跑指定项，如 kettle_9,mixer_9")
    a = ap.parse_args()

    targets = []
    if a.only:
        for t in a.only.split(","):
            fam, idx = t.strip().rsplit("_", 1)
            targets.append((fam, int(idx)))
    else:
        for fam, chain in CHAINS.items():
            for i in range(1, len(chain) + 1):
                targets.append((fam, i))

    print("器具链重绘 %d 张（独立模板）\n" % len(targets))
    for fam, i in targets:
        item = CHAINS[fam][i - 1]
        print("  %-12s %s" % ("%s_%d" % (fam, i), item[:66]))

    if not a.apply:
        print("\n[dry-run] 加 --apply 执行。预估 $%.1f" % (len(targets) * 0.19))
        return

    ok = fail = 0
    for fam, i in targets:
        name = "game_%s_%d.png" % (fam, i)
        item = CHAINS[fam][i - 1]
        prompt = TEMPLATE.format(item=item, short=short_name(item))
        cmd = [sys.executable, SKILL, "t2i", "--style", "bakery-painterly",
               "-p", prompt, "--name", os.path.splitext(name)[0],
               "-s", "1024x1024", "--bg", "transparent", "-o", OUT]
        r = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8",
                           errors="replace", timeout=900)
        good = r.returncode == 0
        ok += good
        fail += (not good)
        print("%s %-18s %s" % ("✅" if good else "❌", name, item[:52]))
        if not good:
            print("   ", ((r.stdout or "") + (r.stderr or ""))[-160:])

    # 尺寸对齐
    print("\n尺寸对齐…")
    for fam, i in targets:
        name = "game_%s_%d.png" % (fam, i)
        p = os.path.join(OUT, name)
        q = os.path.join(SRC, name)
        if os.path.exists(p) and os.path.exists(q):
            t = Image.open(q).size
            im = Image.open(p).convert("RGBA")
            if im.size != t:
                im.resize(t, Image.LANCZOS).save(p)
            print("   %-18s -> %dx%d" % (name, t[0], t[1]))

    print("\n完成 成功%d 失败%d" % (ok, fail))


if __name__ == "__main__":
    main()
