# -*- coding: utf-8 -*-
"""按「经过用途核实」的白名单，把存档里的产物覆盖回项目。

与之前 apply_redraw.py 的区别：
  那份是「全量无差别覆盖」，导致 Tiles/Map/Fonts 被毁。
  这份只覆盖**已核实用途、且产物经人工看图确认可用**的资源。

白名单条目格式：
  (分组, 是否启用, 理由)
"""
import argparse
import json
import os
import shutil
import sys

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
PROJ = os.path.normpath(os.path.join(HERE, ".."))
ART = os.path.join(PROJ, "Assets", "Project Files", "Game", "Art")
ARCH = "D:/claudeWorkbase/redraw_archive_2026-09-12"
BACKUP = "D:/claudeWorkbase/_verified_backup"

# 已验证可覆盖的组（产物来源，分组，说明）
VERIFIED = [
    # Merge 合成链：运行时实测为 MergeItem 的 grades[].sprite；产物递进经看图确认正确
    ("_redraw/Gameplay/Pieces", "Gameplay/Pieces",
     "Merge合成链物品，递进逐级正确、尺寸对齐"),
    # 宝箱：已修正"闭合态"，逐档华丽度递增，经看图确认
    ("_redraw/Gameplay/Chests", "Gameplay/Chests",
     "宝箱，闭合态+5档递进正确"),
    # 角色立绘
    ("_redraw/Gameplay/Characters", "Gameplay/Characters", "角色立绘"),
    # 道具图标
    ("_redraw/Gameplay/PowerUps", "Gameplay/PowerUps", "道具图标"),
    # Dock
    ("_redraw/Gameplay/Dock", "Gameplay/Dock", "Dock底图"),
    # 杂项
    ("_redraw/Gameplay/Misc", "Gameplay/Misc", "杂项玩法图"),
    # Dialog
    ("_redraw/Dialog", "Dialog", "对话UI"),
    # Fx
    ("_redraw/Fx", "Fx", "特效贴图"),
    # 九宫格暖化（保几何，安全）
    ("_redraw_9slice_reskin/UI/Common", "UI/Common", "九宫格暖化（几何未变）"),
    ("_redraw_9slice_reskin/UI/Store", "UI/Store", "商店UI暖化（几何未变）"),
    # UI 内容物（仅可重绘类）
    ("_redraw/UI/Icons", "UI/Icons", "图标内容物"),
]

# 明确排除的「产物来源目录」——这些产出经实测不可用（会毁功能）
BLOCKED_SRC = {
    "_redraw/Gameplay/Tiles",          # 变成空框，失去棋子图案
    "_redraw/Gameplay/Tiles/Special",  # 未验证
    "_redraw/Map",                     # 变成蛋糕塔，无法铺地图
    "_redraw/Fonts",                   # 变成相框，字符丢失
    "_redraw/Scene",                   # 未生成
    "_redraw/UI/Common",               # 九宫格被破坏（改用 reskin 版）
    "_redraw/UI/Store",                # 同上
    "_redraw/UI/Tutorial",             # 符号类
}


def norm(p):
    return p.replace("\\", "/").strip("/")


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    ap = argparse.ArgumentParser()
    ap.add_argument("--apply", action="store_true")
    ap.add_argument("--group", action="append", help="只处理指定目标分组")
    a = ap.parse_args()

    only = {norm(x) for x in (a.group or [])}
    go, skip = [], []

    for src_rel, dst_rel, why in VERIFIED:
        if only and dst_rel not in only:
            continue
        if src_rel in BLOCKED_SRC:
            skip.append((dst_rel, "产物来源在排除名单(%s)" % src_rel, why))
            continue
        if dst_rel in {"Gameplay/Tiles", "Map", "Scene/Backgrounds"}:
            skip.append((dst_rel, "该组用途受限（不可换）", why))
            continue
        sd = os.path.join(ARCH, src_rel)
        if not os.path.isdir(sd):
            skip.append((dst_rel, "产物目录不存在", src_rel))
            continue
        for dp, dn, fn in os.walk(sd):
            for f in sorted(fn):
                if not f.lower().endswith(".png"):
                    continue
                prod = os.path.join(dp, f)
                sub = os.path.relpath(dp, sd).replace(os.sep, "/")
                rel = dst_rel if sub == "." else dst_rel + "/" + sub
                tgt = os.path.join(ART, rel.replace("/", os.sep), f)
                if not os.path.exists(tgt):
                    skip.append((rel + "/" + f, "项目里无此文件", ""))
                    continue
                try:
                    A, B = Image.open(tgt), Image.open(prod)
                except Exception as e:
                    skip.append((rel + "/" + f, "打不开: %s" % e, ""))
                    continue
                if A.size != B.size:
                    skip.append((rel + "/" + f, "尺寸 %s→%s" % (A.size, B.size), ""))
                    continue
                go.append((rel, f, tgt, prod, why))

    print("将覆盖 %d 张，跳过 %d 张\n" % (len(go), len(skip)))
    import collections
    c = collections.Counter(r for r, _, _, _, _ in go)
    for k, v in sorted(c.items()):
        print("   %-28s %3d 张" % (k, v))
    if skip:
        print("\n跳过：")
        for r, w, _ in skip[:12]:
            print("   ⏭  %-40s %s" % (r, w))
        if len(skip) > 12:
            print("   … 另 %d 条" % (len(skip) - 12))

    if not a.apply:
        print("\n[dry-run] 加 --apply 执行")
        return

    os.makedirs(BACKUP, exist_ok=True)
    recs = []
    for rel, f, tgt, prod, why in go:
        bak = os.path.join(BACKUP, rel.replace("/", os.sep), f)
        os.makedirs(os.path.dirname(bak), exist_ok=True)
        shutil.copy2(tgt, bak)
        shutil.copy2(prod, tgt)
        recs.append({"rel": rel, "name": f, "tgt": tgt, "backup": bak, "why": why})

    mp = os.path.join(BACKUP, "manifest.json")
    old = []
    if os.path.exists(mp):
        try:
            old = json.load(open(mp, encoding="utf-8"))
        except Exception:
            old = []
    json.dump(old + recs, open(mp, "w", encoding="utf-8"),
              ensure_ascii=False, indent=2)
    print("\n已覆盖 %d 张" % len(recs))
    print("回滚: python Tools/verified_rollback.py")


if __name__ == "__main__":
    main()
