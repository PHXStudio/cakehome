# -*- coding: utf-8 -*-
"""复核已替换的资源：生成「原图 vs 现图」对比总览图，供逐组肉眼检查。

教训：本次事故中，`Gameplay/Characters` 被从「欧美写实半身像」错绘成
「日系 Q 版萌妹」——因为提示词凭刻板印象写了 "chibi / Q-version"，
且未在看图确认前就批量覆盖。本工具用于事后逐组把关。

用法: python Tools/review_swaps.py [--group 分组] [--per 6] [--out 路径]
"""
import argparse
import collections
import json
import os
import sys

from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
MANIFEST = "D:/claudeWorkbase/_verified_backup/manifest.json"


def norm(p):
    return p.replace("\\", "/").strip("/")


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    ap = argparse.ArgumentParser()
    ap.add_argument("--group")
    ap.add_argument("--per", type=int, default=6)
    ap.add_argument("--out", default="D:/claudeWorkbase/review_swaps.jpg")
    ap.add_argument("--scale", type=int, default=140)
    a = ap.parse_args()

    recs = json.load(open(MANIFEST, encoding="utf-8"))
    by = collections.defaultdict(list)
    for r in recs:
        g = norm(r["rel"])
        if a.group and g != a.group:
            continue
        by[g].append(r)

    S = a.scale
    rows = []
    for g in sorted(by):
        rows.append((g, by[g][:a.per]))

    H = sum(S * 2 + 18 for _ in rows) + 10
    W = S * a.per + 25
    sheet = Image.new("RGB", (W, H), (245, 245, 245))
    d = ImageDraw.Draw(sheet)

    y = 5
    for g, rs in rows:
        d.text((4, y), "%s  (%d 张，展示前 %d)" % (g, len(by[g]), len(rs)),
               fill=(15, 15, 15))
        y += 14
        for i, r in enumerate(rs):
            for k, (p, lab) in enumerate([(r["backup"], "原"), (r["tgt"], "现")]):
                cell = Image.new("RGBA", (S, S), (245, 245, 245, 255))
                if os.path.exists(p):
                    try:
                        im = Image.open(p).convert("RGBA")
                        im.thumbnail((S - 4, S - 4), Image.LANCZOS)
                        cell.alpha_composite(im, ((S - im.width) // 2,
                                                  (S - im.height) // 2))
                    except Exception:
                        pass
                sheet.paste(cell.convert("RGB"), (i * (S + 5), y + k * (S + 3)))
        y += S * 2 + 18

    sheet.save(a.out, quality=85)
    print("每组：上=原图 下=现图   共 %d 组 -> %s  %s"
          % (len(rows), a.out, sheet.size))


if __name__ == "__main__":
    main()
