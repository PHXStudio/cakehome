# -*- coding: utf-8 -*-
"""Zone1 门店改造：把「庭院」素材换成「室内蛋糕店」素材。

机制核实（读 prefab + 代码）：
  Zone 1.prefab 结构 = Background + Building 1~5
  BuildingBehavior 有  defaultSprite + upgrades[].upgradedSprite  → 即每物件 4 档
  prefab 全是 GUID 引用 → **覆盖同名文件即可，prefab 零改动**

5 槽位映射（buildingId / 存档 / 升级成本全不变，只换美术）：
  Building 2  z1_house_*    → counter        (后墙左高柜)
  Building 5  z1_table_*    → dining_table   (圆桌餐椅)
  Building 4  z1_fountain_* → display_shelf  (前景玻璃展示柜)
  Building 1  z1_fence_*    → right_cabinet  (右高柜)
  Building 3  z1_bush_*     → tower_shelf    (三层点心塔)

尺寸处理：原图统一 1024x1024，店内素材尺寸各异 → 等比缩放后居中贴到 1024² 透明画布

用法: python Tools/apply_zone1_shop.py [--apply] [--rollback]
"""
import argparse
import json
import os
import shutil
import sys

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
PROJ = os.path.normpath(os.path.join(HERE, ".."))
ART = os.path.join(PROJ, "Assets", "Project Files", "Game", "Art", "Scene", "Zone1")
SHOP = "D:/claudeWorkbase/_shop_objects/objects"
BACKUP = "D:/claudeWorkbase/_zone1_shop_backup"
MANIFEST = os.path.join(BACKUP, "manifest.json")
OG = (1024, 1024)

# z1 前缀 → 店内素材前缀
MAP = {
    "house": "counter",
    "table": "dining_table",
    "fountain": "display_shelf",
    "fence": "right_cabinet",
    "bush": "tower_shelf",
}
# 背景：整店 4 档（背景不随档位变，用 tier4 豪华版；若需随档可另改）
BG_SRC = "D:/claudeWorkbase/_batch1/_Pending/Zone1Shop/full"
BG_NAME = "game_backgound_zone_1.png"
BG_NAME_PREVIEW = "game_zone_1_preview.png"


def fit(path, out, size=OG):
    """把素材按**内容包围盒**等比放大到画布内，再居中。

    为什么不能直接用原图尺寸居中：
      prefab 有 3 个物件开了 preserveAspect（按图片比例显示）。
      若保留素材四周的透明留白，Unity 会按"含留白"的比例缩放 →
      物件看起来变小、且各槽位比例不一致。
      故先裁掉透明边，再按内容尺寸放大到画布内，保证物件视觉大小一致。
    """
    im = Image.open(path).convert("RGBA")
    # ① 裁掉透明边，取内容包围盒
    bbox = im.getbbox()
    if bbox:
        im = im.crop(bbox)
    # ② 内容等比放大到画布内（留 4% 边距避免贴边）
    pad = 0.96
    s = min(size[0] * pad / im.width, size[1] * pad / im.height)
    nw, nh = max(1, round(im.width * s)), max(1, round(im.height * s))
    cv = Image.new("RGBA", size, (0, 0, 0, 0))
    cv.paste(im.resize((nw, nh), Image.LANCZOS),
             ((size[0] - nw) // 2, (size[1] - nh) // 2))
    cv.save(out)
    return (nw, nh)


def plan_items():
    items = []
    for z, s in MAP.items():
        for i in range(1, 5):
            src = os.path.join(SHOP, "game_background_zone_1_%s_tier%d.png" % (s, i))
            tgt = os.path.join(ART, "z1_%s_%d.png" % (z, i))
            items.append((src, tgt, "%s → %s" % (s, z)))
    # 背景：用 tier4 整店图，等比放进 941x1672
    for tier, label in [(4, "豪华整店")]:
        src = os.path.join(BG_SRC, "game_background_zone_1_full_tier%d.png" % tier)
        tgt = os.path.join(ART, BG_NAME)
        items.append((src, tgt, "整店背景(%s) → game_backgound_zone_1" % label))
    return items


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    ap = argparse.ArgumentParser()
    ap.add_argument("--apply", action="store_true")
    ap.add_argument("--rollback", action="store_true")
    ap.add_argument("--no-bg", action="store_true", help="不换背景，只换 5 物件")
    a = ap.parse_args()

    if a.rollback:
        if not os.path.exists(MANIFEST):
            sys.exit("无备份，无法回滚")
        recs = json.load(open(MANIFEST, encoding="utf-8"))
        n = 0
        for r in recs:
            if os.path.exists(r["backup"]):
                shutil.copy2(r["backup"], r["tgt"])
                n += 1
        print("已回滚 %d 张" % n)
        return

    items = plan_items()
    if a.no_bg:
        items = [i for i in items if BG_NAME not in i[1]]

    print("Zone1 门店改造 · %d 张\n" % len(items))
    for src, tgt, label in items:
        ok = "✓" if os.path.exists(src) else "✗源缺失"
        tgt_size = Image.open(tgt).size if os.path.exists(tgt) else None
        print("  %-34s %-40s %s" % (label, os.path.basename(tgt), ok))

    if not a.apply:
        print("\n[dry-run] 加 --apply 执行（会先备份到 %s）" % BACKUP)
        return

    os.makedirs(BACKUP, exist_ok=True)
    recs = []
    for src, tgt, label in items:
        if not os.path.exists(src) or not os.path.exists(tgt):
            print("  ⏭ 跳过（缺文件）", os.path.basename(tgt))
            continue
        orig_size = Image.open(tgt).size
        bak = os.path.join(BACKUP, os.path.basename(tgt))
        shutil.copy2(tgt, bak)
        if "game_backgound_zone_1.png" == os.path.basename(tgt):
            # 背景不透明，保持原尺寸
            im = Image.open(src).convert("RGB")
            im.resize(orig_size, Image.LANCZOS).save(tgt)
            print("  ✅ %-34s %s → %s(RGB)" % (label, os.path.basename(tgt), orig_size))
        else:
            inner = fit(src, tgt, OG)
            print("  ✅ %-34s %s → 1024² (内容 %dx%d)"
                  % (label, os.path.basename(tgt), inner[0], inner[1]))
        recs.append({"tgt": tgt, "backup": bak, "label": label})

    json.dump(recs, open(MANIFEST, "w", encoding="utf-8"),
              ensure_ascii=False, indent=2)
    print("\n完成：%d 张已替换" % len(recs))
    print("回滚: python Tools/apply_zone1_shop.py --rollback")


if __name__ == "__main__":
    main()
