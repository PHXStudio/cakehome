# -*- coding: utf-8 -*-
"""统计 Art/ 各分组中「在用 / 孤儿 / 已生成」的数量，用于推进全量重绘。

数据源：Tools/art_refs.json（扫描自 prefab/scene 的 GUID 引用）+ 输出目录。

用法: python Tools/art_status.py [--out DIR]
"""
import argparse
import json
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import art_batch_plan as P  # noqa: E402

# 已完成的输出目录 → Art 分组
DONE = {
    "Scene/Backgrounds": "D:/claudeWorkbase/_batch1/Scene/Backgrounds",
    "Scene/Zone2": "D:/claudeWorkbase/_batch1/Scene/Zone2",
    "Scene/Zone3": "D:/claudeWorkbase/_batch1/Scene/Zone3",
    "_Pending/Zone1Shop/full": "D:/claudeWorkbase/_batch1/_Pending/Zone1Shop/full",
    "_Pending/Zone1Shop/objects": "D:/claudeWorkbase/_shop_objects/objects",
}


def norm(p):
    return os.path.abspath(p).replace("\\", "/").lower()


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=None)
    a = ap.parse_args()

    here = os.path.dirname(os.path.abspath(__file__))
    refs = json.load(open(os.path.join(here, "art_refs.json"), encoding="utf-8"))
    used = {norm(p) for p in refs["used"]}
    orphan = {norm(p) for p in refs["orphans"]}

    groups = P.scan()

    def done(group):
        for prefix, outdir in DONE.items():
            if group == prefix or group.startswith(prefix + "/"):
                if os.path.isdir(outdir):
                    return len([f for f in os.listdir(outdir) if f.endswith(".png")])
        return 0

    print("%-36s%6s%6s%6s%7s  %s" % ("分组", "总数", "在用", "孤儿", "已生成", "输出目录"))
    print("-" * 96)
    tu, ta, td = 0, 0, 0
    todo = []
    for k in sorted(groups):
        fs = groups[k]
        nu = sum(1 for f in fs if norm(f) in used)
        no = sum(1 for f in fs if norm(f) in orphan)
        nd = done(k)
        outdir = ""
        for prefix, od in DONE.items():
            if k == prefix or k.startswith(prefix + "/"):
                outdir = od
        print("%-36s%6d%6d%6d%7d  %s" % (k, len(fs), nu, no, nd, outdir))
        tu += nu
        ta += len(fs)
        td += nd
        if nu > 0 and nd < nu:
            todo.append((k, nu - nd))
    print("-" * 96)
    print("合计 %d 张 | 在用 %d | 已生成 %d | 待生成 %d"
          % (ta, tu, td, max(0, tu - td)))
    m = max(0, tu - td)
    print("预估：$%.0f，4 并发约 %.1f 小时（$0.19/张，145s/张）"
          % (m * 0.19, m * 145 / 4 / 3600))
    if todo:
        print("\n待生成分组：")
        for k, n in todo:
            print("   %-36s %d 张" % (k, n))


if __name__ == "__main__":
    main()
