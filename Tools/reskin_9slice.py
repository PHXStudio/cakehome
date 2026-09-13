# -*- coding: utf-8 -*-
"""A1 方案：九宫格 / 几何 UI 基件——保留原图几何，只做暖烘焙调色。

为什么不重绘：
  gpt-image-2 无法产出「中心纯色可拉伸、边缘装饰」的九宫格结构。试了 3 轮提示词，
  它把 "empty center" 理解成「画个凹陷/黑洞」，或产出无圆角的直角矩形。
  九宫格真正的约束是 spriteBorder 边缘像素，重绘必然破坏 → 见 KNOWLEDGE.md
  「img2img 适用边界」中「纯色按钮底」禁止重绘的既有结论。

本脚本做的是「几何 100% 保留 + 色彩暖化」：
  ① alpha 通道原样保留（形状、圆角、边缘装饰全不变）
  ② 色相向暖区间（25°~50°，橙棕）旋转，但保留原有色相差异
     —— 灰色/白色（饱和≈0）不染色，避免整片变黄
  ③ 饱和度压到目标区间，明度轻微回调到基准
  ④ 高光（V>0.97 的近白像素）保护，避免失去立体感

spriteBorder 完全不用动。

用法: python Tools/reskin_9slice.py [--apply]
"""
import argparse
import os
import re
import sys

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ART = os.path.join(HERE, "..", "Assets", "Project Files", "Game", "Art")
OUT = "D:/claudeWorkbase/_redraw_9slice_reskin"

# 目标：对齐 CakeHome 暖烘焙基准（Lab 彩度 38.6 / 明度 77.9）
TARGET_SAT = 0.45          # 目标 HSV 饱和度
WARM_HUE = (0.055, 0.13)   # 暖色相区间（约 20°~47°，橙→暖黄）
GREY_THRESHOLD = 0.08      # 饱和低于此值视为灰/白，不染色
HIGHLIGHT_V = 0.99         # 高光需同时满足：明度极高 AND 饱和度极低（发白）
HIGHLIGHT_S = 0.18         # 只看明度的写法会把「浅蓝/浅紫面板」整片误判为高光


def rgb_to_hsv_np(rgb):
    """rgb: (N,3) float 0..1 → h,s,v"""
    r, g, b = rgb[:, 0], rgb[:, 1], rgb[:, 2]
    mx = np.max(rgb, axis=1)
    mn = np.min(rgb, axis=1)
    d = mx - mn
    v = mx
    s = np.where(mx == 0, 0, d / np.maximum(mx, 1e-9))
    h = np.zeros_like(mx)
    idx = d > 1e-9
    if idx.any():
        rr, gg, bb = r[idx], g[idx], b[idx]
        dd, mxz = d[idx], mx[idx]
        hr = np.where(mxz == rr, ((gg - bb) / dd) % 6, 0.0)
        hg = np.where(mxz == gg, (bb - rr) / dd + 2, 0.0)
        hb = np.where(mxz == bb, (rr - gg) / dd + 4, 0.0)
        h[idx] = (hr + hg + hb) / 6.0
    return h, s, v


def hsv_to_rgb_np(h, s, v):
    i = np.floor(h * 6.0)
    f = h * 6.0 - i
    p = v * (1 - s)
    q = v * (1 - f * s)
    t = v * (1 - (1 - f) * s)
    i = (i % 6).astype(int)
    out = np.zeros((len(h), 3), dtype=np.float64)
    for k, (rr, gg, bb) in enumerate([(v, t, p), (q, v, p), (p, v, t),
                                      (p, q, v), (t, p, v), (v, p, q)]):
        m = i == k
        out[m, 0], out[m, 1], out[m, 2] = rr[m], gg[m], bb[m]
    return out


def warm_shift(h, colored_mask=None):
    """把所有有彩色像素统一到同一个暖色相。

    实测教训：按钮/面板的垂直渐变里，色相会随明度漂移（如 btn_green 从 149°
    漂到 92°）。若保留这个漂移，暖化后会出现"上橙下绿"的断层 —— 那种漂移是
    原始配色的设计噪音，不是要保留的设计意图。故整体统一色相，只保留明度层次。
    """
    center = (WARM_HUE[0] + WARM_HUE[1]) / 2.0
    return np.full_like(h, center)


def reskin(path, out_path, sat_scale=1.0):
    im = Image.open(path).convert("RGBA")
    a = np.asarray(im).astype(np.float64)
    h, w, _ = a.shape
    alpha = a[..., 3].copy()

    flat = a[..., :3].reshape(-1, 3) / 255.0
    hh, ss, vv = rgb_to_hsv_np(flat)

    # ① 灰色/白色：不染色，只轻微提亮
    grey = ss < GREY_THRESHOLD
    # ② 有彩色：色相暖化 + 饱和度归位
    new_h = warm_shift(hh)
    new_s = np.full_like(ss, TARGET_SAT)
    new_v = np.clip(vv * 1.02 + 0.01, 0, 1)

    # ③ 高光保护：必须「明度极高」且「饱和度极低」（即发白的镜面部分）。
    #    只看明度会把浅色面板（V=1.0 的浅蓝/浅紫）整片跳过 —— 实测 panel_blue 100% 被误判。
    hl = (vv > HIGHLIGHT_V) & (ss < HIGHLIGHT_S)

    out = hsv_to_rgb_np(np.where(grey, hh, new_h),
                        np.where(grey, ss, new_s),
                        np.where(grey, vv, new_v))
    out[hl] = flat[hl]                                   # 高光原样

    rgb = (out * 255.0).reshape(h, w, 3)
    res = np.concatenate([rgb, alpha[..., None]], axis=2)
    Image.fromarray(np.clip(res, 0, 255).astype(np.uint8), "RGBA").save(out_path)


# 这些属于"内容物"，走 AI 重绘（见 gen_ui_content.py），不能在这里做调色，
# 否则会把立体细节（如金币星徽）压平。
SKIP = {"ic_coins_pack_1.png", "ic_coins_pack_2.png", "ic_coins_pack_3.png",
        "coin.png", "heart.png", "home icon.png", "ui_icon_coin.png"}


def targets():
    a = []
    for grp in ["UI/Common", "UI/Store"]:
        d = os.path.join(ART, grp)
        if not os.path.isdir(d):
            continue
        for f in sorted(os.listdir(d)):
            if not f.endswith(".png") or f in SKIP:
                continue
            # 已判定为"可重绘"的内容物由 gen_ui_content.py 负责，这里跳过
            try:
                sys.path.insert(0, HERE)
                from ui_classify import classify
                if classify(f, os.path.join(d, f))[1] == "可重绘":
                    continue
            except Exception:
                pass
            a.append((grp, f, os.path.join(d, f)))
    return a


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    ap = argparse.ArgumentParser()
    ap.add_argument("--apply", action="store_true")
    ap.add_argument("--only", help="只处理指定文件，逗号分隔")
    a = ap.parse_args()

    items = targets()
    if a.only:
        want = set(x.strip() for x in a.only.split(","))
        items = [i for i in items if i[1] in want]

    print("九宫格/几何资源暖化调色 · %d 张\n" % len(items))
    for grp, f, p in items:
        im = Image.open(p)
        print("  %-30s %-12s" % (grp + "/" + f, "%dx%d" % im.size))

    if not a.apply:
        print("\n[dry-run] 加 --apply 执行")
        return

    for grp, f, p in items:
        d = os.path.join(OUT, grp)
        os.makedirs(d, exist_ok=True)
        reskin(p, os.path.join(d, f))
    print("\n完成，产物在 %s" % OUT)


if __name__ == "__main__":
    main()
