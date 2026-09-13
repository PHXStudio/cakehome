# -*- coding: utf-8 -*-
"""按 prefab 的真实 rect 拼合预览三场景室内店铺（1:1 还原运行时长宽比）。

为什么需要：坐标表（设计文档第三节）是手算的，必须先看拼合效果
才能确认「不重叠、不浮空、前景遮中景、后墙件不被中景件切掉」。
本工具不调 API、不花钱，纯离线合成。

模拟逻辑与运行时一致：
  - 参考分辨率 1080x1920，中心为原点，UI 坐标 y 向上
  - 每个部件按 rect 等比显示（preserveAspect=1）后贴到对应位置
  - 按 sibling 顺序（Background→1→2→3→4→5）依次叠加，后画的盖住先画的

用法:
  python Tools/preview_zone_room.py --zone z1
  python Tools/preview_zone_room.py --zone z1 --tier 4
  python Tools/preview_zone_room.py --zone all
"""
import argparse
import os
import sys

from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
PROJ = os.path.normpath(os.path.join(HERE, ".."))
ART = os.path.join(PROJ, "Assets", "Project Files", "Game", "Art", "Scene")
OUT = "D:/claudeWorkbase/_zone_preview"

BG = (1080, 1920)
# 与 apply_zone_rooms.py 保持同步：4+1 布局，物件底边落在各 Zone 实测墙地交界线
JUNCTION = {"z1": -230, "z2": -326, "z3": -230}
ROW_DROP = 18
BACK_ROW = {"1": 0, "2": -364, "4": 364}
BACK_BOX = (352, 405)
FRONT_ROW = {"3": -250, "5": 250}
FRONT_BOX = (460, 520)
FRONT_BOTTOM = -840


def layout_for(zone_key):
    out = {}
    bj = JUNCTION[zone_key] - ROW_DROP
    bw, bh = BACK_BOX
    for m, cx in BACK_ROW.items():
        out[m] = (cx, bj + bh / 2, bw, bh)
    fw, fh = FRONT_BOX
    for m, cx in FRONT_ROW.items():
        out[m] = (cx, FRONT_BOTTOM + fh / 2, fw, fh)
    return out

ZONES = {
    "z1": dict(folder="Zone1Shop", prefix="z1shop", label="Zone1 面包房",
               sem={"1": "counter", "2": "shelf", "3": "table", "4": "bar",
                    "5": "tiered_stand"}),
    "z2": dict(folder="Zone2Shop", prefix="z2shop", label="Zone2 咖啡吧",
               sem={"1": "cupboard", "2": "bottle_shelf", "3": "stools",
                    "4": "plant_stand", "5": "menu_board"}),
    "z3": dict(folder="Zone3Shop", prefix="z3shop", label="Zone3 温室茶室",
               sem={"1": "display", "2": "tea_sideboard", "3": "rattan_sofa",
                    "4": "flower_stand", "5": "tiered_stand"}),
}


def place(canvas, sprite_path, cx, cy, bw, bh):
    """把 sprite 按 preserveAspect 放进 (bw x bh) 的框并居中贴到 (cx, cy)。"""
    if not os.path.exists(sprite_path):
        return False
    im = Image.open(sprite_path).convert("RGBA")
    bbox = im.getbbox()
    if bbox:
        im = im.crop(bbox)
    s = min(bw / im.width, bh / im.height)
    nw, nh = max(1, round(im.width * s)), max(1, round(im.height * s))
    im = im.resize((nw, nh), Image.LANCZOS)
    # UI 坐标 y 向上 → 画布 y 向下
    px = round(BG[0] / 2 + cx - nw / 2)
    py = round(BG[1] / 2 - cy - nh / 2)
    canvas.alpha_composite(im, (px, py))
    return True


def render(zone_key, tier, mark=True):
    z = ZONES[zone_key]
    d = os.path.join(ART, z["folder"])
    bg = os.path.join(d, "%s_background.png" % z["prefix"])
    canvas = Image.new("RGBA", BG, (250, 250, 250, 255))
    have_bg = os.path.exists(bg)
    if have_bg:
        canvas.alpha_composite(Image.open(bg).convert("RGBA").resize(BG, Image.LANCZOS))

    lay = layout_for(zone_key)
    missing = []
    for m in "12345":
        p = os.path.join(d, "%s_building%s_%s_%d.png" % (z["prefix"], m, z["sem"][m], tier))
        cx, cy, bw, bh = lay[m]
        if not place(canvas, p, cx, cy, bw, bh):
            missing.append("building_%s %s" % (m, z["sem"][m]))

    if mark:
        dr = ImageDraw.Draw(canvas)
        # 实测墙地交界线
        jy = int(BG[1] / 2 - JUNCTION[zone_key])
        dr.line([(0, jy), (BG[0], jy)], fill=(255, 0, 0, 200), width=3)
        dr.text((6, jy + 4), "wall/floor junction", fill=(255, 0, 0))
        for m in "12345":
            cx, cy, bw, bh = lay[m]
            px = BG[0] / 2 + cx - bw / 2
            py = BG[1] / 2 - cy - bh / 2
            dr.rectangle([px, py, px + bw, py + bh], outline=(0, 120, 255, 170), width=2)
            dr.text((px + 4, py + 4), "b%s" % m, fill=(0, 90, 220))
    return canvas, missing, have_bg


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    ap = argparse.ArgumentParser()
    ap.add_argument("--zone", default="all")
    ap.add_argument("--tier", type=int, default=1)
    ap.add_argument("--no-mark", action="store_true")
    ap.add_argument("--out", default=OUT)
    a = ap.parse_args()

    keys = sorted(ZONES) if a.zone == "all" else [x.strip() for x in a.zone.split(",")]
    os.makedirs(a.out, exist_ok=True)

    # 三张并排 + 每张单独存
    S = 420
    H = int(S * BG[1] / BG[0])
    sheet = Image.new("RGB", (S * len(keys) + 5 * (len(keys) + 1), H + 24), (238, 238, 238))
    dr = ImageDraw.Draw(sheet)
    x = 5
    for k in keys:
        im, missing, have_bg = render(k, a.tier, not a.no_mark)
        im.convert("RGB").resize((S, H), Image.LANCZOS)
        sheet.paste(im.convert("RGB").resize((S, H), Image.LANCZOS), (x, 20))
        dr.text((x + 4, 5), "%s  tier%d%s" % (ZONES[k]["label"], a.tier,
                                             "" if have_bg else "  [缺背景]"),
                fill=(0, 0, 0))
        p = os.path.join(a.out, "%s_tier%d.png" % (k, a.tier))
        im.convert("RGB").save(p)
        print("[%s] → %s" % (k, p))
        if missing:
            print("     缺 %d 个部件: %s" % (len(missing), ", ".join(missing)))
        x += S + 5
    sp = os.path.join(a.out, "all_tier%d.png" % a.tier)
    sheet.save(sp)
    print("\n拼合总览 → %s  %s" % (sp, sheet.size))


if __name__ == "__main__":
    main()
