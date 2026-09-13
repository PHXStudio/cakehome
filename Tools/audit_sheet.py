# -*- coding: utf-8 -*-
"""生成「每组原图 vs 现图」对比总览图，供人工逐组肉眼确认。

指标审计（audit_redraw.py）会漏判——实测 Tiles 那批「5 张全变空框」相似度
只有 0.53 看着正常，指标抓不到，必须看图。

用法: python Tools/audit_sheet.py [--per 4] [--out 路径]
"""
import argparse
import collections
import json
import os
import sys

from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
ART = os.path.join(HERE, "..", "Assets", "Project Files", "Game", "Art")
BACKUP = "D:/claudeWorkbase/_redraw_backup"
MANIFEST = os.path.join(BACKUP, "manifest.json")


def norm(p):
    return p.replace("\\", "/").strip("/")


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    ap = argparse.ArgumentParser()
    ap.add_argument("--per", type=int, default=4, help="每组取几张")
    ap.add_argument("--out", default="D:/claudeWorkbase/audit_all.jpg")
    ap.add_argument("--scale", type=int, default=110)
    a = ap.parse_args()

    recs = json.load(open(MANIFEST, encoding="utf-8"))
    by = collections.defaultdict(list)
    for r in recs:
        by[norm(r["rel"])].append(r["name"])

    S = a.scale
    rows = [(rel, sorted(by[rel])[:a.per]) for rel in sorted(by)]
    H = len(rows) * (S * 2 + 16) + 10
    sheet = Image.new("RGB", (S * a.per + 25, H), (245, 245, 245))
    d = ImageDraw.Draw(sheet)

    y = 5
    for rel, fs in rows:
        for i, f in enumerate(fs):
            for r, base in enumerate([BACKUP, ART]):
                cell = Image.new("RGBA", (S, S), (245, 245, 245, 255))
                p = os.path.join(base, rel, f)
                if os.path.exists(p):
                    im = Image.open(p).convert("RGBA")
                    im.thumbnail((S - 4, S - 4), Image.LANCZOS)
                    cell.alpha_composite(im, ((S - im.width) // 2,
                                              (S - im.height) // 2))
                sheet.paste(cell.convert("RGB"), (i * (S + 6), y + r * (S + 3)))
        d.text((4, y + S * 2 + 3), "%-34s (%d张)" % (rel, len(by[rel])),
               fill=(15, 15, 15))
        y += S * 2 + 16

    sheet.save(a.out, quality=82)
    print("每组：上排=原图  下排=现在   共 %d 组 -> %s  %s"
          % (len(rows), a.out, sheet.size))


if __name__ == "__main__":
    main()
