# -*- coding: utf-8 -*-
"""统计 Art/ 各分组的张数、尺寸分布、透明资源占比，用于规划分批重绘。"""
import os
import sys
import collections

from PIL import Image

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                    "..", "Assets", "Project Files", "Game", "Art")


def scan():
    groups = collections.defaultdict(list)
    for dp, dn, fn in os.walk(ROOT):
        for f in fn:
            if not f.lower().endswith(".png"):
                continue
            rel = os.path.relpath(dp, ROOT).replace(os.sep, "/")
            groups[rel].append(os.path.join(dp, f))
    return groups


def meta(files):
    sizes = collections.Counter()
    alpha = 0
    for p in files:
        try:
            im = Image.open(p)
            sizes[im.size] += 1
            if im.mode in ("RGBA", "LA", "P"):
                if im.convert("RGBA").getchannel("A").getextrema()[0] < 250:
                    alpha += 1
        except Exception:
            pass
    return sizes, alpha


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    groups = scan()
    rows = []
    total = 0
    for g in sorted(groups):
        fs = groups[g]
        sizes, alpha = meta(fs)
        main_size = sizes.most_common(1)[0][0]
        rows.append((g, len(fs), "%dx%d" % main_size, alpha, len(sizes)))
        total += len(fs)

    print("%-36s%6s%13s%10s%10s" % ("分组", "张数", "主流尺寸", "带透明", "尺寸种类"))
    print("-" * 78)
    for g, n, s, a, k in rows:
        print("%-36s%6d%13s%10s%10d" % (g, n, s, "%d/%d" % (a, n), k))
    print("-" * 78)
    print("%-36s%6d" % ("合计", total))


if __name__ == "__main__":
    main()
