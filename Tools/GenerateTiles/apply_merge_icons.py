#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
把合成线（Merge）物品图标应用为三消棋子贴图。

- 源图：Assets/Project Files/Game/Images/Gameplay/game_*.png（1024，含 alpha 拖影垃圾）
- 清理：缩小 alpha → 阈值 → 从中心洪水填充取主体 → 填洞 → 放大回原尺寸软边
- 处理：裁包围盒 → 留 4% 边距 → 居中到正方形 → 缩到 512
- 输出：覆盖 Assets/.../Images/Tiles/tile_c?_t?.png（文件名/GUID 不变，prefab 无需改）

用法：
  python3 Tools/GenerateTiles/apply_merge_icons.py --preview   # 只生成带文件名标签的预览表
  python3 Tools/GenerateTiles/apply_merge_icons.py             # 正式覆盖 Tiles
"""

import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from collections import deque
import os
import sys

GAMEPLAY = os.path.join(os.path.dirname(__file__), "..", "..",
                        "Assets", "Project Files", "Game", "Images", "Gameplay")
TILES_DIR = os.path.join(os.path.dirname(__file__), "..", "..",
                         "Assets", "Project Files", "Game", "Images", "Tiles")
OUT = 512
MARGIN = 0.04          # 四周留白比例

# 棋子 → 合成图标（三消每关用一组 5 枚，组内颜色/轮廓必须易区分）
MAPPING = {
    # Collection 1 —— 蛋糕链（烘焙）
    "tile_c1_t1.png": "game_cake_3.png",     # 海绵蛋糕胚（黄）
    "new tile_c1_t1.png": "game_cake_3.png",  # 同上（prefab 变体）
    "tile_c1_t2.png": "game_cake_7.png",     # 草莓挞（红）
    "tile_c1_t3.png": "game_cake_6.png",     # 蓝莓玛芬（橙+蓝）
    "tile_c1_t4.png": "game_cake_9.png",     # 提拉米苏（棕方）
    "tile_c1_t5.png": "game_cake_4.png",     # 曲奇（橙褐圆）
    # Collection 2 —— 咖啡链
    "tile_c2_t1.png": "game_coffe_1.png",    # 咖啡豆（棕）
    "tile_c2_t2.png": "game_coffe_5.png",    # 橙马克杯（橙）
    "tile_c2_t3.png": "game_coffe_8.png",    # 蓝杯拿铁（蓝）
    "tile_c2_t4.png": "game_coffe_10.png",   # 薄荷冰咖啡（棕+绿）
    "tile_c2_t5.png": "game_coffe_9.png",    # 猫咪拿铁（奶白+蓝）
    # Collection 3 —— 糖果 + 汽水链
    "tile_c3_t1.png": "game_candy_3.png",    # 棒棒糖（粉）
    "tile_c3_t2.png": "game_candy_1.png",    # 紫软糖（紫）
    "tile_c3_t3.png": "game_candy_2.png",    # 蓝包装糖（蓝）
    "tile_c3_t4.png": "game_candy_4.png",    # 糖果罐（多彩）
    "tile_c3_t5.png": "game_soda_3.png",     # 可乐瓶（红棕）
}


def clean_alpha(img):
    """去掉主体轮廓之外的 alpha 拖影垃圾；返回干净 RGBA numpy"""
    a = np.asarray(img.convert("RGBA")).copy()
    alpha = a[..., 3]

    # 1/4 分辨率上做形态学：细拖影被平均掉，主体保留
    small = img.split()[3].resize((img.width // 4, img.height // 4), Image.BILINEAR)
    mask = np.asarray(small) > 100
    h, w = mask.shape

    # 从中心洪水填充（图标主体都在中心），取连通主体
    seed = (h // 2, w // 2)
    if not mask[seed]:
        ys, xs = np.nonzero(mask)                       # 中心恰好在缝里则找最近实体像素
        d = (ys - seed[0]) ** 2 + (xs - seed[1]) ** 2
        seed = (ys[d.argmin()], xs[d.argmin()])
    comp = np.zeros_like(mask)
    comp[seed] = True
    q = deque([seed])
    while q:
        y, x = q.popleft()
        for ny, nx in ((y-1, x), (y+1, x), (y, x-1), (y, x+1)):
            if 0 <= ny < h and 0 <= nx < w and mask[ny, nx] and not comp[ny, nx]:
                comp[ny, nx] = True
                q.append((ny, nx))

    # 填主体内部的洞（如杯口内侧）
    outside = np.zeros_like(mask)
    q = deque()
    for x in range(w):
        for y in (0, h - 1):
            if not comp[y, x] and not outside[y, x]:
                outside[y, x] = True; q.append((y, x))
    for y in range(h):
        for x in (0, w - 1):
            if not comp[y, x] and not outside[y, x]:
                outside[y, x] = True; q.append((y, x))
    while q:
        y, x = q.popleft()
        for ny, nx in ((y-1, x), (y+1, x), (y, x-1), (y, x+1)):
            if 0 <= ny < h and 0 <= nx < w and not comp[ny, nx] and not outside[ny, nx]:
                outside[ny, nx] = True
                q.append((ny, nx))
    body = ~outside                                     # 主体 + 内洞

    # 放大回原尺寸，软边，乘到原 alpha 上
    keep = Image.fromarray((body * 255).astype(np.uint8)).resize(img.size, Image.BILINEAR)
    keep = keep.filter(ImageFilter.GaussianBlur(2))
    k = np.asarray(keep).astype(np.float64) / 255.0
    a[..., 3] = (alpha * k).astype(np.uint8)
    a[a[..., 3] == 0] = [0, 0, 0, 0]                    # 透明区 RGB 清零，防 mipmap 渗色
    return a


def fit_square(a, margin=MARGIN):
    """裁包围盒 → 加边距居中到正方形 → 512"""
    ys, xs = np.nonzero(a[..., 3] > 8)
    if len(ys) == 0:
        return a
    y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    crop = a[y0:y1, x0:x1]
    ch, cw = crop.shape[:2]
    side = int(max(ch, cw) / (1 - margin * 2))
    canvas = np.zeros((side, side, 4), dtype=np.uint8)
    oy, ox = (side - ch) // 2, (side - cw) // 2
    canvas[oy:oy + ch, ox:ox + cw] = crop
    img = Image.fromarray(canvas, "RGBA").resize((OUT, OUT), Image.LANCZOS)
    return np.asarray(img)


def process(src_name):
    img = Image.open(os.path.join(GAMEPLAY, src_name))
    return fit_square(clean_alpha(img))


def main():
    preview_only = "--preview" in sys.argv
    done = {}
    cells = []
    for tile_name, src_name in MAPPING.items():
        if src_name not in done:
            done[src_name] = process(src_name)
            cells.append((src_name, done[src_name]))
        if not preview_only:
            Image.fromarray(done[src_name]).save(os.path.join(TILES_DIR, tile_name))
            print("  ✓", tile_name, "←", src_name)

    # 预览表：每个源图一格，底下写文件名（校验 Read 结果乱序问题）
    cols = 5
    rows = (len(cells) + cols - 1) // cols
    cell = 300
    sheet = Image.new("RGBA", (cols * cell, rows * (cell + 40)), (58, 52, 60, 255))
    draw = ImageDraw.Draw(sheet)
    for i, (name, a) in enumerate(cells):
        x, y = (i % cols) * cell, (i // cols) * (cell + 40)
        t = Image.fromarray(a).resize((cell, cell), Image.LANCZOS)
        sheet.paste(t, (x, y), t)
        draw.text((x + 8, y + cell + 8), name, fill=(255, 255, 255, 255))
    out = os.path.join(os.path.dirname(__file__), "preview", "_merge_icons_sheet.png")
    os.makedirs(os.path.dirname(out), exist_ok=True)
    sheet.save(out)
    print("预览表:", out)


if __name__ == "__main__":
    main()
