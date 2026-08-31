#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Zone 1 场景巴西市场风格调色 —— 鲜艳/高饱和/暖金阳光/热带绿。

对同一张图内所有像素做统一 grade（保持 alpha 不变）：
1. 暖白平衡（R+ B-）
2. 自然饱和度（vibrance：低饱和像素提得多，避免过曝区炸色）
3. S 曲线对比
4. 高光暖金（阳光感）
5. 热带绿加深 + 天空偏青

用法：
  python3 Tools/GenerateTiles/grade_brazil.py --preview   # 只出 before/after 对比图
  python3 Tools/GenerateTiles/grade_brazil.py             # 覆盖原文件（git 可回滚）
"""

import numpy as np
from PIL import Image
import glob
import os
import sys

IMG = os.path.join(os.path.dirname(__file__), "..", "..",
                   "Assets", "Project Files", "Game", "Images")

FILES = (
    [os.path.join(IMG, "game_zone_background.png"),
     os.path.join(IMG, "Zone 1", "game_backgound_zone_1.png")]
    + [p for p in glob.glob(os.path.join(IMG, "Zone 1", "z1_*.png"))
       if "_shadow" not in p]          # 阴影图不调色，避免染色
)


def grade(path):
    a = np.asarray(Image.open(path).convert("RGBA")).astype(np.float64) / 255.0
    rgb = a[..., :3]
    alpha = a[..., 3:]

    # 1. 暖白平衡
    rgb = rgb * np.array([1.06, 1.01, 0.93])

    # 2. vibrance 自然饱和（低饱和提 32%，高饱和少提）
    lum = (rgb * np.array([0.299, 0.587, 0.114])).sum(-1, keepdims=True)
    mx = rgb.max(-1, keepdims=True)
    mn = rgb.min(-1, keepdims=True)
    sat = np.where(mx > 0.01, (mx - mn) / np.maximum(mx, 1e-5), 0)
    boost = 1.0 + 0.34 * (1 - sat)
    rgb = lum + (rgb - lum) * boost

    # 3. S 曲线对比
    rgb = np.clip(rgb, 0, 1)
    rgb = rgb + 0.35 * (rgb * rgb * (3 - 2 * rgb) - rgb)

    # 4. 高光暖金（lum > 0.65 的受光面加阳光色）
    hl = np.clip((np.squeeze(lum) - 0.65) / 0.25, 0, 1)[..., None] ** 1.5
    rgb = rgb + hl * np.array([0.055, 0.018, -0.03])

    # 5. 热带绿加深（G 主导像素）
    greenish = np.clip((rgb[..., 1] - np.maximum(rgb[..., 0], rgb[..., 2])) / 0.12, 0, 1)
    rgb[..., 1] = np.minimum(rgb[..., 1] * (1 + 0.07 * greenish), 1)
    rgb[..., 0] = rgb[..., 0] * (1 - 0.03 * greenish)

    # 6. 天空偏青（B 主导且明亮）
    skyish = np.clip((rgb[..., 2] - np.maximum(rgb[..., 0], rgb[..., 1])) / 0.10, 0, 1) * np.clip(np.squeeze(lum) * 1.4, 0, 1)
    rgb[..., 1] = np.minimum(rgb[..., 1] + 0.05 * skyish, 1)

    out = np.concatenate([np.clip(rgb, 0, 1), alpha], axis=-1)
    return Image.fromarray((out * 255).astype(np.uint8), "RGBA")


def main():
    preview_only = "--preview" in sys.argv
    pairs = []
    for path in FILES:
        if not os.path.exists(path):
            continue
        before = Image.open(path).convert("RGBA")
        after = grade(path)
        pairs.append((os.path.basename(path), before, after))
        if not preview_only:
            after.save(path)
            print("  ✓", os.path.basename(path))

    # 对比表：左原图右调色后，只放背景大图 + 代表性道具
    show = [p for p in pairs if "backgound" in p[0] or "background" in p[0]
            or "house_4" in p[0] or "fountain_4" in p[0] or "table_1" in p[0]]
    th = 560
    sheet = Image.new("RGBA", (th * 2 + 12, (th + 30) * len(show)), (40, 38, 44, 255))
    from PIL import ImageDraw
    d = ImageDraw.Draw(sheet)
    y = 0
    for name, before, after in show:
        bw = int(before.width * th / before.height)
        aw = int(after.width * th / after.height)
        sheet.paste(before.resize((bw, th), Image.LANCZOS), (0, y), before.resize((bw, th), Image.LANCZOS))
        sheet.paste(after.resize((aw, th), Image.LANCZOS), (th + 12, y), after.resize((aw, th), Image.LANCZOS))
        d.text((8, y + th + 6), f"{name}   左=原版 右=巴西风", fill=(255, 255, 255, 255))
        y += th + 30
    out = os.path.join(os.path.dirname(__file__), "preview", "_brazil_grade_sheet.png")
    os.makedirs(os.path.dirname(out), exist_ok=True)
    sheet.save(out)
    print("对比表:", out, f"（共处理 {len(pairs)} 张）")


if __name__ == "__main__":
    main()
