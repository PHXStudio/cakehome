# -*- coding: utf-8 -*-
"""修正 .meta 的 maxTextureSize 限制，使其不小于 PNG 实际尺寸。

背景（实测发现）：
  大量资源的 .meta 里 `platformSettings[0].maxTextureSize` 被设成很小的值
  （Pieces 只有 256、UI/Common 有 128/32 的），Unity 导入时会把大图强制缩小。
  例如我替换的 1024² 合成链物品，Unity 里实际只有 256² —— 细节全丢。

原则：
  maxTextureSize 应 >= PNG 实际尺寸，否则等于浪费生成成本、画面还变糊。
  但不能盲目设 2048（内存/包体代价），所以**按实际 PNG 尺寸取最近的 2 的幂**。

用法:
  python Tools/fix_max_texture_size.py                 # dry-run，列出将修改的
  python Tools/fix_max_texture_size.py --apply
  python Tools/fix_max_texture_size.py --group Gameplay/Pieces --apply
"""
import argparse
import collections
import os
import re
import sys

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ART = os.path.join(HERE, "..", "Assets", "Project Files", "Game", "Art")
# 已是合理值则不动
MIN_OK = {64, 128, 256, 512, 1024, 2048, 4096, 8192}


def next_pow2_at_least(n):
    p = 64
    while p < n and p < 8192:
        p *= 2
    return p


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    ap = argparse.ArgumentParser()
    ap.add_argument("--apply", action="store_true")
    ap.add_argument("--group", help="只处理某分组")
    a = ap.parse_args()

    todo = []
    for dp, dn, fn in os.walk(ART):
        grp = os.path.relpath(dp, ART).replace(os.sep, "/")
        if a.group and grp != a.group:
            continue
        for f in sorted(fn):
            if not f.endswith(".png.meta"):
                continue
            png = os.path.join(dp, f[:-5])
            meta = os.path.join(dp, f)
            if not os.path.exists(png):
                continue
            try:
                w, h = Image.open(png).size
            except Exception:
                continue
            need = next_pow2_at_least(max(w, h))
            t = open(meta, encoding="utf-8", errors="replace").read()
            m = re.search(r"buildTarget:\s*DefaultTexturePlatform\s*\n\s*"
                          r"maxTextureSize:\s*(\d+)", t)
            if not m:
                continue
            cur = int(m.group(1))
            if cur >= max(w, h):
                continue                      # 已够大，不动
            todo.append((grp, f[:-5], (w, h), cur, need, meta))

    by = collections.defaultdict(list)
    for grp, name, sz, cur, need, meta in todo:
        by[grp].append((name, sz, cur, need))

    print("需提升 maxTextureSize 的资源：%d 张\n" % len(todo))
    for grp in sorted(by):
        rs = by[grp]
        print("  %-28s %3d 张" % (grp, len(rs)))
        for name, sz, cur, need in rs[:3]:
            print("       %-34s PNG=%dx%d  限制 %d → %d"
                  % (name, sz[0], sz[1], cur, need))
        if len(rs) > 3:
            print("       … 另 %d 张" % (len(rs) - 3))

    if not a.apply:
        print("\n[dry-run] 加 --apply 执行")
        return

    n = 0
    for grp, name, sz, cur, need, meta in todo:
        t = open(meta, encoding="utf-8", errors="replace").read()
        t2 = re.sub(r"(buildTarget:\s*DefaultTexturePlatform\s*\n\s*"
                    r"maxTextureSize:\s*)\d+", r"\g<1>%d" % need, t)
        if t2 != t:
            open(meta, "w", encoding="utf-8", newline="\n").write(t2)
            n += 1
    print("\n已修改 %d 个 .meta" % n)
    print("Unity 需重新导入：可以 send refresh 或重启编辑器")


if __name__ == "__main__":
    main()
