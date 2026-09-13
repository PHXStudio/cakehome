# -*- coding: utf-8 -*-
"""UI 资源分类：判定哪些可 AI 重绘、哪些必须保留几何。

判定依据（来自 KNOWLEDGE.md「img2img 适用边界」实测教训）：
  ① 九宫格       —— .meta 里 spriteBorder 非零 → 边缘像素被拉伸，重绘必错位 → 禁止
  ② 几何符号     —— 箭头/叉/加号/开关/锁 等纯几何 → AI 会加装饰、画丢符号 → 禁止
  ③ 纯色/渐变底  —— 无内容物的背景、渐变、光晕 → 加了装饰就是污染 → 禁止
  ④ 内容物       —— 金币/宝石/礼包/道具等有实体的图 → 可重绘
  ⑤ 图文贴图     —— 含文字的贴图（ui_text_*）→ 重绘会改字 → 禁止

用法: python Tools/ui_classify.py [--json]
"""
import argparse
import json
import os
import re
import sys

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ART = os.path.join(HERE, "..", "Assets", "Project Files", "Game", "Art")
GROUPS = ["UI/Common", "UI/Icons", "UI/Store", "UI/Tutorial"]

# 几何符号关键词 → 禁止重绘
SYMBOL_WORDS = ("arrow", "cross", "close", "check", "plus", "minus", "play",
                "pause", "skip", "toggle", "tgl_", "switch", "lock", "question",
                "settings", "gear", "undo", "indicator", "pointer", "hammer",
                "trash", "video", "no_ads", "noads", "infinity", "construction")
# 纯底/渐变/光效关键词 → 禁止重绘
PLATE_WORDS = ("panel", "btn_", "ui_button", "gradient", "grad_", "shine",
               "spark", "blinder", "frame", "background_", "ui_back",
               "ui_fill_bar", "ui_header", "item_background", "ui_panel",
               "ui_cup_silhouette", "ui_energy_collage", "badge_", "header_",
               "ui_shine", "click_effect")
# 含文字贴图 → 禁止重绘
TEXT_WORDS = ("ui_text_", "logo")


def sprite_border(png_path):
    """读 .meta 的 spriteBorder，返回 (l,b,r,t) 或 None。"""
    meta = png_path + ".meta"
    if not os.path.exists(meta):
        return None
    t = open(meta, encoding="utf-8", errors="replace").read()
    m = re.search(r"spriteBorder:\s*\{x:\s*([-\d.]+),\s*y:\s*([-\d.]+),"
                  r"\s*z:\s*([-\d.]+),\s*w:\s*([-\d.]+)\}", t)
    if not m:
        return None
    v = tuple(float(x) for x in m.groups())
    return v if any(x > 0 for x in v) else None


def classify(name, path):
    low = name.lower()
    b = sprite_border(path)
    if b:
        return "九宫格", "禁止", "spriteBorder=%s，边缘被拉伸，重绘必错位" % (
            tuple(round(x) for x in b),)
    if any(w in low for w in TEXT_WORDS):
        return "图文贴图", "禁止", "含文字/标志，重绘会改字面"
    if any(w in low for w in SYMBOL_WORDS):
        return "几何符号", "禁止", "AI 会加装饰并画丢/画乱符号"
    if any(w in low for w in PLATE_WORDS):
        return "纯底/渐变", "禁止", "无内容物的底图，加装饰即污染"
    return "内容物", "可重绘", "有实体内容，重绘后仍是同类物件"


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    ap = argparse.ArgumentParser()
    ap.add_argument("--json", action="store_true")
    a = ap.parse_args()

    rows = []
    for grp in GROUPS:
        d = os.path.join(ART, grp)
        if not os.path.isdir(d):
            continue
        for f in sorted(os.listdir(d)):
            if not f.lower().endswith(".png"):
                continue
            p = os.path.join(d, f)
            kind, verdict, why = classify(f, p)
            im = Image.open(p)
            rows.append({"group": grp, "name": f, "kind": kind,
                         "verdict": verdict, "why": why, "size": "%dx%d" % im.size})

    if a.json:
        print(json.dumps(rows, ensure_ascii=False, indent=2))
        return

    for grp in GROUPS:
        rs = [r for r in rows if r["group"] == grp]
        if not rs:
            continue
        ok = [r for r in rs if r["verdict"] == "可重绘"]
        no = [r for r in rs if r["verdict"] == "禁止"]
        print("=" * 78)
        print("%s   共 %d 张 → 可重绘 %d / 禁止 %d" % (grp, len(rs), len(ok), len(no)))
        print("=" * 78)
        if ok:
            print("  ✅ 可重绘：")
            for r in ok:
                print("      %-34s %-10s %s" % (r["name"], r["size"], r["kind"]))
        if no:
            print("  ⛔ 禁止重绘：")
            for r in no:
                print("      %-34s %-10s %s" % (r["name"], r["kind"], r["why"][:38]))
        print()

    tot_ok = sum(1 for r in rows if r["verdict"] == "可重绘")
    print("合计 %d 张：可重绘 %d，禁止 %d  → 重绘成本 $%.1f"
          % (len(rows), tot_ok, len(rows) - tot_ok, tot_ok * 0.19))


if __name__ == "__main__":
    main()
