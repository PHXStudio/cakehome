#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
底栏 Tab 图标生成器 —— 自主设计，皮克斯软糖质感（与合成素材风格统一）。

- ui_icon_store.png → 纸杯蛋糕（咖啡店=卖蛋糕，品牌感）
- ui_icon_map.png   → 指南针（区域地图=探索方向）

自包含 SDF 光照渲染器（1024 超采样 → 512 输出），覆盖同名文件（GUID 不变，引用零改动）。
用法：python3 Tools/GenerateTiles/generate_nav_icons.py [--preview]
"""

import numpy as np
from PIL import Image, ImageDraw, ImageFilter
import os
import sys

RENDER = 1024
OUT = 512
IMAGES_DIR = os.path.join(os.path.dirname(__file__), "..", "..",
                          "Assets", "Project Files", "Game", "Images")

_yy, _xx = np.mgrid[0:RENDER, 0:RENDER].astype(np.float64) / RENDER


# ─── SDF 基元 ───────────────────────────────────────────────────────────────

def sd_circle(cx, cy, r):
    return np.hypot(_xx - cx, _yy - cy) - r


def sd_ellipse(cx, cy, rx, ry, rot=0.0):
    c, s = np.cos(rot), np.sin(rot)
    dx, dy = _xx - cx, _yy - cy
    px = c * dx + s * dy
    py = -s * dx + c * dy
    d = np.hypot(px / rx, py / ry) - 1.0
    return d * min(rx, ry)


def sd_poly(points, round_r=0.0):
    n = len(points)
    d = np.full(_xx.shape, 1e9)
    inside = np.zeros(_xx.shape, dtype=bool)
    for i in range(n):
        x1, y1 = points[i]
        x2, y2 = points[(i + 1) % n]
        ex, ey = x2 - x1, y2 - y1
        wx, wy = _xx - x1, _yy - y1
        t = np.clip((wx * ex + wy * ey) / (ex * ex + ey * ey + 1e-12), 0, 1)
        bx = wx - ex * t
        by = wy - ey * t
        d = np.minimum(d, bx * bx + by * by)
        crosses = ((y1 > _yy) != (y2 > _yy))
        xint = x1 + (_yy - y1) * (x2 - x1) / (y2 - y1 + 1e-12)
        inside ^= crosses & (_xx < xint)
    return np.where(inside, -1.0, 1.0) * np.sqrt(d) - round_r


def smin(a, b, k):
    h = np.clip(0.5 + 0.5 * (b - a) / k, 0, 1)
    return b * (1 - h) + a * h - k * h * (1 - h)


# ─── 颜色 ───────────────────────────────────────────────────────────────────

def rgb(r, g, b):
    return np.array([r, g, b], dtype=np.float64) / 255.0


def vgrad(shape, top_col, bot_col, y0=0.15, y1=0.9):
    t = np.clip((_yy - y0) / (y1 - y0), 0, 1)[..., None]
    return np.ones(shape + (3,)) * (top_col * (1 - t) + bot_col * t)


# ─── 渲染核心（与棋子同款光照）──────────────────────────────────────────────

class Item:
    def __init__(self):
        self.parts = []
        self.silhouette = None

    def add(self, sdf, color, h_scale=1.0, inset=False, spec_mul=1.0, depth=0.55):
        self.parts.append((sdf, color, h_scale, inset, spec_mul, depth))
        if not inset:
            self.silhouette = sdf if self.silhouette is None else np.minimum(self.silhouette, sdf)
        return self

    def render(self, specular=0.9, sheen=0.20, dome=26.0, spot=0.22):
        px = 1.0 / RENDER
        H = np.zeros(_xx.shape)
        col = np.zeros(_xx.shape + (3,))
        spec_mask = np.zeros(_xx.shape)

        for sdf, color, h_scale, inset, spec_mul, depth in self.parts:
            edge = -sdf
            prof = np.clip(edge / (dome * px), 0, 1)
            h_part = np.sqrt(np.maximum(prof * (2 - prof), 0))
            c = color if isinstance(color, np.ndarray) else np.ones(_xx.shape + (3,)) * color
            if inset:
                H = H * (1 - depth * h_part * h_scale)
                m = np.clip(prof * 1.2, 0, 1)[..., None]
                col = col * (1 - m * 0.75) + c * m * 0.75
                spec_mask *= (1 - 0.7 * np.squeeze(m))
            else:
                H = np.maximum(H, h_part * h_scale)
                m = np.clip(edge / (2.5 * px), 0, 1)[..., None]
                col = col * (1 - m) + c * m
                spec_mask = np.maximum(spec_mask, np.clip(edge / (2.5 * px), 0, 1) * spec_mul)

        gy, gx = np.gradient(H)
        z = 1.0 / (dome * 0.55)
        nx, ny, nz = -gx * z, -gy * z, np.ones(_xx.shape)
        nl = np.sqrt(nx * nx + ny * ny + nz * nz)
        nx, ny, nz = nx / nl, ny / nl, nz / nl

        L = np.array([-0.42, -0.58, 0.70]); L /= np.linalg.norm(L)
        diff = np.clip(nx * L[0] + ny * L[1] + nz * L[2], 0, 1)
        L2 = np.array([0.55, 0.60, 0.45]); L2 /= np.linalg.norm(L2)
        bounce = np.clip(nx * L2[0] + ny * L2[1] + nz * L2[2], 0, 1)
        edge_factor = 1 - np.clip(H, 0, 1)
        bounce *= edge_factor * 0.6

        amb = 0.52 + 0.18 * np.clip(1 - _yy, 0, 1)
        lit = col * (amb + diff * 0.62 + bounce * 0.35)[..., None]

        V = np.array([0, 0, 1.0])
        Hv = (L + V) / np.linalg.norm(L + V)
        nh = np.clip(nx * Hv[0] + ny * Hv[1] + nz * Hv[2], 0, 1)
        spec = (np.power(nh, 90) * specular + np.power(nh, 8) * sheen) * spec_mask
        lit += spec[..., None]

        if spot > 0:
            s = np.exp(-(((_xx - 0.38) / 0.16) ** 2 + ((_yy - 0.34) / 0.11) ** 2))
            lit += (s * spot * np.clip(H, 0, 1) * spec_mask)[..., None]

        lit *= (1 - 0.22 * np.clip(edge_factor ** 3, 0, 1))[..., None]

        alpha = np.clip(-self.silhouette / (1.6 * px), 0, 1)
        return np.concatenate([np.clip(lit, 0, 1.4), alpha[..., None]], axis=-1)


def drop_shadow(rgba, dy=14, blur=18, alpha=0.25):
    sil = (rgba[..., 3] * 255).astype(np.uint8)
    img = Image.fromarray(sil).filter(ImageFilter.GaussianBlur(blur))
    sh = np.asarray(img).astype(np.float64) / 255.0
    sh = np.roll(sh, dy, axis=0)
    out = rgba.copy()
    shadow_rgb = np.array([0.16, 0.10, 0.08])
    sa = (sh * alpha * (1 - rgba[..., 3]))[..., None]
    out[..., :3] = rgba[..., :3] * (1 - sa) + shadow_rgb * sa
    out[..., 3] = np.clip(rgba[..., 3] + np.squeeze(sa), 0, 1)
    return out


def save(rgba, path):
    img = Image.fromarray((np.clip(rgba, 0, 1) * 255).astype(np.uint8), "RGBA")
    img = img.resize((OUT, OUT), Image.LANCZOS)
    img.save(path)
    print("  ✓", os.path.basename(path))


# ─── 两个图标设计 ───────────────────────────────────────────────────────────
# 主题：咖啡店经营 + 露营车旅行。两枚图标统一设计语言：
# 正面平视构图、奶油+粉+暖棕配色、软糖体积光。

def icon_cafe():
    """咖啡店门面：粉白条纹遮阳棚（浅波浪边）+ 奶油墙 + 木门"""
    it = Item()
    # 墙体（顶部伸进棚内，避免缝隙）
    wall = sd_poly([(0.24, 0.36), (0.76, 0.36), (0.76, 0.86), (0.24, 0.86)], round_r=0.03)
    wcol = vgrad(_xx.shape, rgb(255, 246, 228), rgb(238, 214, 178))
    it.add(wall, wcol, h_scale=0.85, spec_mul=0.7)
    # 木门（圆顶门）
    door = smin(sd_poly([(0.41, 0.60), (0.59, 0.60), (0.59, 0.86), (0.41, 0.86)], round_r=0.015),
                sd_circle(0.5, 0.60, 0.09), 0.01)
    dcol = vgrad(_xx.shape, rgb(196, 128, 74), rgb(146, 88, 44))
    it.add(door, dcol, h_scale=0.6, spec_mul=0.8)
    it.add(sd_circle(0.55, 0.72, 0.014), rgb(255, 220, 130), h_scale=0.35, spec_mul=1.2)
    # 遮阳棚：顶部圆角长条 - 底部浅波浪（小圆轻咬）
    awn = sd_poly([(0.17, 0.16), (0.83, 0.16), (0.83, 0.40), (0.17, 0.40)], round_r=0.05)
    scallop = None
    for i in range(6):
        c = sd_circle(0.17 + 0.66 * (i + 0.5) / 6, 0.40, 0.66 / 12 * 0.62)
        scallop = c if scallop is None else np.minimum(scallop, c)
    awn = np.maximum(awn, -scallop + 0.035)
    # 粉白竖条纹
    stripe = (np.sin((_xx - 0.17) / 0.66 * np.pi * 6) > 0)
    pink = vgrad(_xx.shape, rgb(255, 150, 190), rgb(236, 84, 140))
    white = vgrad(_xx.shape, rgb(255, 252, 246), rgb(244, 230, 214))
    acol = np.where(stripe[..., None], pink, white)
    it.add(awn, acol, h_scale=0.95, spec_mul=1.1)
    return it


def icon_camper():
    """露营车：圆弧车顶 + 粉色腰线 + 蓝窗 + 双轮（侧视）"""
    it = Item()
    # 车轮（先画，压在车身下）
    wcol = vgrad(_xx.shape, rgb(96, 88, 92), rgb(48, 44, 50))
    hcol = vgrad(_xx.shape, rgb(230, 226, 222), rgb(170, 166, 162))
    for cx in (0.32, 0.68):
        it.add(sd_circle(cx, 0.72, 0.085), wcol, h_scale=0.7, spec_mul=0.9)
        it.add(sd_circle(cx, 0.72, 0.042), hcol, h_scale=0.5, spec_mul=1.1)
    # 车身：下盒 + 圆弧车顶（收窄避免两侧鼓包）
    body = sd_poly([(0.14, 0.40), (0.86, 0.40), (0.86, 0.68), (0.14, 0.68)], round_r=0.05)
    body = smin(body, sd_ellipse(0.5, 0.40, 0.28, 0.115), 0.035)
    cream = vgrad(_xx.shape, rgb(255, 250, 238), rgb(240, 226, 200))
    pink = vgrad(_xx.shape, rgb(255, 150, 190), rgb(236, 84, 140))
    bcol = np.where((_yy > 0.54)[..., None], pink, cream)   # 粉色腰线
    it.add(body, bcol, h_scale=0.9, spec_mul=1.0)
    # 车窗（深蓝玻璃，一大一小）
    win1 = sd_poly([(0.24, 0.42), (0.44, 0.42), (0.44, 0.54), (0.24, 0.54)], round_r=0.022)
    win2 = sd_poly([(0.54, 0.42), (0.72, 0.42), (0.72, 0.54), (0.54, 0.54)], round_r=0.022)
    vcol = vgrad(_xx.shape, rgb(150, 214, 245), rgb(70, 140, 195))
    it.add(smin(win1, win2, 0.005), vcol, h_scale=0.55, spec_mul=1.4)
    # 车头小灯
    it.add(sd_circle(0.845, 0.60, 0.022), rgb(255, 224, 130), h_scale=0.45, spec_mul=1.2)
    return it


ICONS = {
    "ui_icon_store.png": icon_cafe,
    "ui_icon_map.png": icon_camper,
}


def main():
    preview_only = "--preview" in sys.argv
    out_dir = os.path.join(os.path.dirname(__file__), "preview") if preview_only else IMAGES_DIR
    os.makedirs(out_dir, exist_ok=True)

    cells = []
    for name, fn in ICONS.items():
        rgba = fn().render()
        rgba = drop_shadow(rgba)
        save(rgba, os.path.join(out_dir, name))
        cells.append((name, rgba))

    cell = 320
    sheet = Image.new("RGBA", (len(cells) * cell, cell + 40), (58, 52, 60, 255))
    d = ImageDraw.Draw(sheet)
    for i, (name, rgba) in enumerate(cells):
        t = Image.fromarray((np.clip(rgba, 0, 1) * 255).astype(np.uint8), "RGBA")
        t = t.resize((cell, cell), Image.LANCZOS)
        sheet.paste(t, (i * cell, 0), t)
        d.text((i * cell + 8, cell + 8), name, fill=(255, 255, 255, 255))
    out = os.path.join(out_dir, "_nav_icons_sheet.png")
    sheet.save(out)
    print("预览表:", out)


if __name__ == "__main__":
    main()
