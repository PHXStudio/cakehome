# -*- coding: utf-8 -*-
"""从 _redraw_backup 回退指定分组的资源（其余保留）。

用法: python Tools/rollback_group.py Map [MoreGroups...]
"""
import json
import os
import shutil
import sys

MANIFEST = "D:/claudeWorkbase/_redraw_backup/manifest.json"


def norm(p):
    return p.replace("\\", "/").strip("/")


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    if len(sys.argv) < 2:
        sys.exit("用法: python Tools/rollback_group.py <分组> [分组...]")
    want = {norm(x) for x in sys.argv[1:]}

    recs = json.load(open(MANIFEST, encoding="utf-8"))
    hit = [r for r in recs if norm(r["rel"]) in want]
    print("manifest 共 %d 条，匹配 %d 条" % (len(recs), len(hit)))

    n = 0
    for r in hit:
        src = r["src_meta"][:-5]          # 去掉 .meta
        if os.path.exists(r["backup"]):
            shutil.copy2(r["backup"], src)
            n += 1
            print("   回退 %-24s %s" % (r["rel"], r["name"]))
    print("已回退 %d 张" % n)

    keep = [r for r in recs if norm(r["rel"]) not in want]
    json.dump(keep, open(MANIFEST, "w", encoding="utf-8"),
              ensure_ascii=False, indent=2)
    print("manifest 更新：%d → %d" % (len(recs), len(keep)))


if __name__ == "__main__":
    main()
