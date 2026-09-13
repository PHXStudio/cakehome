# -*- coding: utf-8 -*-
"""把 _redraw/ 的重绘产物对位覆盖回 Assets/.../Art/。

安全机制：
  ① 白名单 —— UI 分组只覆盖 ui_classify 判定为「可重绘」的；几何/九宫格/纯底一律跳过
  ② 尺寸校验 —— 产物尺寸必须与原图一致，否则跳过（避免拉伸）
  ③ 透明校验 —— 原图有透明区而产物无 → 报警跳过
  ④ 备份 —— 覆盖前把原文件复制到 _redraw_backup/（另可用 git 回滚到基线 9624f3a）
  ⑤ 清单 —— 生成 manifest 记录每张的替换来源，便于回滚

用法:
  python Tools/apply_redraw.py                 # dry-run，列出将覆盖的
  python Tools/apply_redraw.py --apply         # 执行覆盖
  python Tools/apply_redraw.py --rollback      # 从 _redraw_backup 回滚
"""
import argparse
import json
import os
import shutil
import sys

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from ui_classify import classify, GROUPS as UI_GROUPS  # noqa: E402

ART = os.path.join(HERE, "..", "Assets", "Project Files", "Game", "Art")
OUT = "D:/claudeWorkbase/_redraw"
# 暖化调色的几何/九宫格产物（reskin_9slice.py 输出）——它们与 OUT 同等对待，
# 但 UI 白名单校验对它们不需要（本来就只处理禁止重绘的那批）
OUT_RESKIN = "D:/claudeWorkbase/_redraw_9slice_reskin"
BACKUP = "D:/claudeWorkbase/_redraw_backup"
MANIFEST = "D:/claudeWorkbase/_redraw_backup/manifest.json"


def has_alpha(p):
    try:
        im = Image.open(p)
        if im.mode not in ("RGBA", "LA", "P"):
            return False
        return im.convert("RGBA").getchannel("A").getextrema()[0] < 250
    except Exception:
        return False


def collect():
    """扫描产物，按安全规则筛选出可覆盖项。"""
    go, skip = [], []
    # ① 重绘产物（走 UI 白名单校验）
    for dp, dn, fn in os.walk(OUT):
        for f in fn:
            if not f.lower().endswith(".png"):
                continue
            rel = os.path.relpath(dp, OUT).replace(os.sep, "/")
            src = os.path.join(ART, rel.replace("/", os.sep), f)
            prod = os.path.join(dp, f)
            if not os.path.exists(src):
                skip.append((rel, f, "原文件不存在"))
                continue
            # ① UI 分组白名单
            if rel in UI_GROUPS:
                kind, verdict, why = classify(f, src)
                if verdict != "可重绘":
                    skip.append((rel, f, "UI %s：%s" % (kind, why[:26])))
                    continue
            # ② 尺寸
            a, b = Image.open(src), Image.open(prod)
            if a.size != b.size:
                skip.append((rel, f, "尺寸不符 %s→%s" % (a.size, b.size)))
                continue
            # ③ 透明
            if has_alpha(src) and not has_alpha(prod):
                skip.append((rel, f, "原图有透明区，产物无"))
                continue
            go.append((rel, f, src, prod))
    # ② 暖化调色产物（几何保留，白名单天然通过）
    for dp, dn, fn in os.walk(OUT_RESKIN):
        for f in fn:
            if not f.lower().endswith(".png"):
                continue
            rel = os.path.relpath(dp, OUT_RESKIN).replace(os.sep, "/")
            src = os.path.join(ART, rel.replace("/", os.sep), f)
            prod = os.path.join(dp, f)
            if not os.path.exists(src):
                skip.append((rel, f, "原文件不存在"))
                continue
            a_, b_ = Image.open(src), Image.open(prod)
            if a_.size != b_.size:
                skip.append((rel, f, "尺寸不符"))
                continue
            go.append((rel, f, src, prod))
    return go, skip


def do_apply(go):
    os.makedirs(BACKUP, exist_ok=True)
    recs = []
    for rel, f, src, prod in go:
        bak = os.path.join(BACKUP, rel.replace("/", os.sep), f)
        os.makedirs(os.path.dirname(bak), exist_ok=True)
        shutil.copy2(src, bak)          # ④ 备份
        shutil.copy2(prod, src)         # 覆盖
        recs.append({"rel": rel, "name": f,
                     "src_meta": src + ".meta", "backup": bak})
    # ⑤ 清单（合并已有，保留历史）
    old = []
    if os.path.exists(MANIFEST):
        try:
            old = json.load(open(MANIFEST, encoding="utf-8"))
        except Exception:
            old = []
    json.dump(old + recs, open(MANIFEST, "w", encoding="utf-8"),
              ensure_ascii=False, indent=2)
    return recs


def do_rollback():
    if not os.path.exists(MANIFEST):
        sys.exit("没有 manifest，无法回滚")
    recs = json.load(open(MANIFEST, encoding="utf-8"))
    n = 0
    for r in recs:
        if os.path.exists(r["backup"]):
            shutil.copy2(r["backup"], r["src_meta"][:-5])   # 去掉 .meta
            n += 1
    print("已回滚 %d 张" % n)


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    ap = argparse.ArgumentParser()
    ap.add_argument("--apply", action="store_true")
    ap.add_argument("--rollback", action="store_true")
    ap.add_argument("--show-skip", action="store_true", help="显示被跳过的项")
    a = ap.parse_args()

    if a.rollback:
        do_rollback()
        return

    go, skip = collect()
    print("将覆盖 %d 张，跳过 %d 张\n" % (len(go), len(skip)))
    for rel, f, src, prod in go:
        print("   ✅ %-30s %s" % (rel, f))
    if a.show_skip:
        print("\n跳过的：")
        for rel, f, why in skip:
            print("   ⏭  %-30s %-30s %s" % (rel, f, why))

    if not a.apply:
        print("\n[dry-run] 加 --apply 执行覆盖（会先备份到 %s）" % BACKUP)
        return

    recs = do_apply(go)
    print("\n已覆盖 %d 张，备份在 %s" % (len(recs), BACKUP))
    print("回滚：python Tools/apply_redraw.py --rollback")
    print("或 git 回滚：cd %s && git checkout 9624f3a -- \"Assets/Project Files/Game/Art\"" % HERE)


if __name__ == "__main__":
    main()
