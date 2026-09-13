# -*- coding: utf-8 -*-
"""把已生成的部件从「强制正方形画布」重处理为「按内容原始长宽比」。

为什么必须改（实测教训）：
  之前 fit_square() 把内容居中贴到 1024x1024 透明画布，
  于是 sprite 的 rect 长宽比恒为 1:1（含透明留白）。
  Unity 的 Image + preserveAspect 是按 **sprite rect** 等比缩放 →
  实际显示尺寸 = rect 的**较小边**，柜子再高也显示不出高度，
  结果就是「物件偏小、上方大片空墙」。

修正：
  裁掉透明边后，画布尺寸 = 内容长宽比（长边固定 1024），内容刚好填满画布。
  这样 sprite 的 rect 长宽比 = 内容真实长宽比 →
  在竖向 rect（如 330x560）里，高柜能顶满高度，视觉占比立刻变大。

本脚本 **不调用任何 API**（$0），只是把 _zone_items/ 里的原始产物重新裁切输出。
幂等：可重复执行。

用法: python Tools/refit_zone_items.py [--max 1024]
"""
import argparse
import os
import sys

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
PROJ = os.path.normpath(os.path.join(HERE, ".."))
ART = os.path.join(PROJ, "Assets", "Project Files", "Game", "Art", "Scene")
RAW = "D:/claudeWorkbase/_zone_items"

ZONES = {
    "z1": dict(folder="Zone1Shop", prefix="z1shop",
               sem={"1": "counter", "2": "shelf", "3": "table", "4": "bar",
                    "5": "tiered_stand"}),
    "z2": dict(folder="Zone2Shop", prefix="z2shop",
               sem={"1": "cupboard", "2": "bottle_shelf", "3": "stools",
                    "4": "plant_stand", "5": "menu_board"}),
    "z3": dict(folder="Zone3Shop", prefix="z3shop",
               sem={"1": "display", "2": "tea_sideboard", "3": "rattan_sofa",
                    "4": "flower_stand", "5": "tiered_stand"}),
}


def refit(src, dst, longest=1024):
    """裁透明边 → 按内容长宽比铺满画布（长边 = longest），内容无留白。"""
    im = Image.open(src).convert("RGBA")
    bbox = im.getbbox()
    if bbox:
        im = im.crop(bbox)
    s = longest / max(im.width, im.height)
    nw, nh = max(1, round(im.width * s)), max(1, round(im.height * s))
    im = im.resize((nw, nh), Image.LANCZOS)
    cv = Image.new("RGBA", (nw, nh), (0, 0, 0, 0))
    cv.paste(im, (0, 0))
    # 尺寸取偶数，避免 Unity 压缩产生 1px 边条
    if nw % 2 or nh % 2:
        nw2, nh2 = nw + (nw % 2), nh + (nh % 2)
        cv2 = Image.new("RGBA", (nw2, nh2), (0, 0, 0, 0))
        cv2.paste(cv, (0, 0))
        cv, nw, nh = cv2, nw2, nh2
    cv.save(dst)
    return (nw, nh)


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    ap = argparse.ArgumentParser()
    ap.add_argument("--longest", type=int, default=1024)
    ap.add_argument("--only")
    a = ap.parse_args()

    keys = sorted(ZONES)
    if a.only:
        keys = [x.strip() for x in a.only.split(",")]

    n = 0
    share = {}
    for zk in keys:
        z = ZONES[zk]
        d = os.path.join(ART, z["folder"])
        for m in "12345":
            stem = "%s_building%s_%s" % (z["prefix"], m, z["sem"][m])
            for t in range(1, 5):
                dst = os.path.join(d, "%s_%d.png" % (stem, t))
                # 原始产物命名：t1 无后缀，t2..t4 带 _N
                cands = [os.path.join(RAW, zk, "%s_%d.png" % (stem, t) if t > 1
                                      else "%s.png" % stem),
                         os.path.join(RAW, zk, "%s.png" % stem)]
                src = next((c for c in cands if os.path.exists(c)), None)
                if not src:
                    # 已在 Art 目录的（如 t1 首次生成的 stem.png 被改名过）
                    if os.path.exists(dst):
                        src = dst
                    else:
                        print("  ⚠ 缺原始产物 %s_t%d" % (stem, t))
                        continue
                sz = refit(src, dst, a.longest)
                share.setdefault((end_ratio(sz)), 0)
                share[end_ratio(sz)] += 1
                n += 1
                print("  ✅ %-40s t%d  %dx%d  (高宽比 %.2f)"
                      % (stem, t, sz[0], sz[1], sz[1] / sz[0]))

    print("\n已重处理 %d 张（长边 %d，内容铺满画布，无透明留白）"
          % (n, a.longest))
    if share:
        print("高宽比分布：%s" % sorted("%.2f×%d" % (k[0], v) for k, v in share.items()))


def end_ratio(sz):
    return (round(sz[1] / sz[0], 2),)


if __name__ == "__main__":
    main()
