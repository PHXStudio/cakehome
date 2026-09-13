# -*- coding: utf-8 -*-
"""事故审计：检查已覆盖的重绘资源是否「失去了原有区分度」。

判据（基于两次实际事故）：
  ① 细节密度 —— 梯度幅度骤降 >40% 说明图案被抹平（tile 变空框的事故）
  ② 组内相似度 —— 同组资源两两相似度过高，说明被画成了同一个东西
     （tiles 那批 5 张全变成一样的框，玩家无法区分）
  ③ 尺寸/透明一致性 —— 与备份对比

用法: python Tools/audit_redraw.py [--group 分组]
"""
import collections
import json
import os
import sys

import numpy as np
from PIL import Image

ART = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                   "..", "Assets", "Project Files", "Game", "Art")
BACKUP = "D:/claudeWorkbase/_redraw_backup"
MANIFEST = os.path.join(BACKUP, "manifest.json")


def norm(p):
    return p.replace("\\", "/").strip("/")


def detail(p, n=128):
    """图像细节密度（平均梯度）。图案被抹平会显著降低。"""
    im = Image.open(p).convert("L").resize((n, n))
    a = np.asarray(im).astype(float)
    return (np.abs(np.diff(a, 1, axis=1)).mean() +
            np.abs(np.diff(a, 1, axis=0)).mean()) / 2


def fingerprint(p, n=64):
    """缩略图指纹，用于算组内相似度。"""
    im = Image.open(p).convert("RGB").resize((n, n))
    return np.asarray(im).astype(float).flatten()


def similarity(fps):
    """组内两两平均相似度（归一化互相关）。"""
    if len(fps) < 2:
        return None
    vals = []
    for i in range(len(fps)):
        for j in range(i + 1, len(fps)):
            a, b = fps[i] - fps[i].mean(), fps[j] - fps[j].mean()
            d = np.linalg.norm(a) * np.linalg.norm(b)
            if d > 1e-9:
                vals.append(float(a @ b / d))
    return sum(vals) / len(vals) if vals else None


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    only = None
    if "--group" in sys.argv:
        only = norm(sys.argv[sys.argv.index("--group") + 1])

    recs = json.load(open(MANIFEST, encoding="utf-8"))
    by = collections.defaultdict(list)
    for r in recs:
        by[norm(r["rel"])].append(r["name"])

    print("%-30s%6s%10s%10s%11s  %s"
          % ("分组", "张数", "原细节", "现细节", "组内相似", "判定"))
    print("-" * 92)
    todo = []
    for rel in sorted(by):
        if only and rel != only:
            continue
        names = by[rel]
        d_now, d_old, fps = [], [], []
        for f in names:
            p_now = os.path.join(ART, rel, f)
            p_old = os.path.join(BACKUP, rel, f)
            if os.path.exists(p_now) and os.path.exists(p_old):
                d_now.append(detail(p_now))
                d_old.append(detail(p_old))
                fps.append(fingerprint(p_now))
        if not d_now:
            continue
        on, nn = sum(d_old) / len(d_old), sum(d_now) / len(d_now)
        sim = similarity(fps)
        chg = (nn - on) / max(on, 1e-6) * 100

        flags = []
        if chg < -40:
            flags.append("细节骤降")
        if sim is not None and sim > 0.90:
            flags.append("组内雷同")
        verdict = ("🔴 " + "、".join(flags)) if flags else "✅"
        sim_s = "%.2f" % sim if sim is not None else "—"
        print("%-30s%6d%10.2f%10.2f%11s  %s"
              % (rel, len(d_now), on, nn, sim_s, verdict))
        if flags:
            todo.append(rel)

    if todo:
        print("\n需人工复查的分组：")
        for t in todo:
            print("   ", t)
        print("\n回退命令： python Tools/rollback_group.py \"%s\"" % "\" \"".join(todo))


if __name__ == "__main__":
    main()
