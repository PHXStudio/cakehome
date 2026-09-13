# -*- coding: utf-8 -*-
"""Zone1 烘焙店内：5 物件 × 4 档独立生成（透明 PNPG），用于与整店背景拼合。

设计约束（来自 KNOWLEDGE.md 2026-09-06 方案，buildingId/存档/成本不变，只换美术）：
  building_1 (house)    → 后墙左高柜    800x900
  building_2 (fountain) → 前景玻璃展示柜 1000x1300
  building_3 (table)    → 圆桌餐椅      750x1000
  building_4 (fence)    → 右高柜        850x1100
  building_5 (bush)     → 三层点心塔    500x900

视角统一：正立面平视（flat front view, eye-level），保证与整店背景可拼合。
背景：完全透明，无地面、无投影、无光晕。

用法: python Tools/gen_shop_objects.py [--apply] [--limit N] [--jobs N] [--tier N] [--rmbg]
"""
import argparse
import concurrent.futures as cf
import os
import subprocess
import sys

SKILL = os.path.expanduser("~/.claude/skills/gpt-image-2/gpt_image_2.py")
RMBG = "D:/claudeWorkbase/models_rmbg/rmbg14.onnx"
OUT = "D:/claudeWorkbase/_shop_objects"
API_SIZES = [(1024, 1024), (1024, 1536), (1536, 1024), (2048, 2048)]

# 统一视角与背景约束（拼合的关键）
VIEW = ("Flat front elevation view, eye-level straight-on, the object faces the viewer "
        "squarely with no perspective foreshortening and no tilt, symmetrical, centered. "
        "Isolated cutout on a completely transparent background: absolutely NO ground "
        "plane, NO floor, NO grass, NO backdrop, NO cast shadow, NO glow, NO vignette. "
        "Hand-painted casual game illustration, rich material texture, warm key light "
        "from the upper front, soft ambient occlusion inside crevices only. "
        "Warm caramel-brown, cream and soft pastel palette. No text, no UI, no characters.")

# 槽位 → (文件名前缀, 尺寸, 物件基础描述)
SLOTS = {
    "counter": ("后墙左高柜", (800, 900),
                "a tall back-wall display cabinet for a bakery, filled with cakes and "
                "pastries on its shelves"),
    "display_shelf": ("前景玻璃展示柜", (1000, 1300),
                      "a glass-front refrigerated display case for a bakery, showing "
                      "cakes and desserts inside"),
    "dining_table": ("圆桌餐椅", (750, 1000),
                     "a small round cafe table with two chairs, for bakery customers"),
    "right_cabinet": ("右高柜", (850, 1100),
                      "a tall right-side storage cabinet with open shelves and drawers, "
                      "for a bakery"),
    "tower_shelf": ("三层点心塔", (500, 900),
                    "a three-tier dessert tower stand displaying cakes and pastries"),
}

# 四档升级：从简朴到豪华，材质与装饰递进
TIERS = [
    "tier 1, starter shop: plain simple white-cream painted finish, minimal decoration, "
    "basic and modest, a few items only",
    "tier 2, local bakery: warm natural wood construction, carved panel details, fuller "
    "and more inviting, more items on display",
    "tier 3, premium boutique: rich dark wood with gold trim accents, glass elements, "
    "elegant carvings, luxurious and well stocked",
    "tier 4, grand patisserie: ornate cream and gold French style, marble surfaces, "
    "chandelier-lit glamour, glass and gilded details, opulent and fully stocked",
]


def pick_api_size(w, h):
    ar = w / float(h)

    def matched(s):
        return abs(s[0] / s[1] - ar) <= 0.02

    cands = [s for s in API_SIZES if matched(s)]
    if cands:
        enough = [s for s in cands if s[0] * s[1] >= w * h]
        return "%dx%d" % (enough[0] if enough else cands[0])
    return "%dx%d" % min(API_SIZES, key=lambda s: abs(s[0] / s[1] - ar))


def rmbg(path):
    try:
        import numpy as np
        import onnxruntime as ort
        from PIL import Image
        sess = ort.InferenceSession(RMBG, providers=["CPUExecutionProvider"])
        nm = sess.get_inputs()[0].name
        orig = Image.open(path).convert("RGBA")
        flat = Image.new("RGB", orig.size, (255, 255, 255))
        flat.paste(orig, (0, 0), orig)
        flat = flat.resize((1024, 1024), Image.LANCZOS)
        arr = np.asarray(flat).astype(np.float32) / 255.0 - 0.5
        m = sess.run(None, {nm: arr.transpose(2, 0, 1)[None]})[0][0, 0]
        m = np.clip(m, 0, 1)
        m = (m - m.min()) / (m.max() - m.min() + 1e-8)
        mi = Image.fromarray((m * 255).astype(np.uint8)).resize(orig.size, Image.LANCZOS)
        orig.putalpha(mi)
        orig.save(path)
        return True
    except Exception as e:
        print("      RMBG 失败:", str(e)[:120])
        return False


def fit_to(path, w, h):
    from PIL import Image
    im = Image.open(path).convert("RGBA")
    if im.size == (w, h):
        return
    src_ar, dst_ar = im.width / im.height, w / float(h)
    if abs(src_ar - dst_ar) < 1e-3:
        im.resize((w, h), Image.LANCZOS).save(path)
        return
    scale = min(w / im.width, h / im.height)
    nw, nh = max(1, round(im.width * scale)), max(1, round(im.height * scale))
    cv = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    cv.paste(im.resize((nw, nh), Image.LANCZOS), ((w - nw) // 2, (h - nh) // 2))
    cv.save(path)


def build_items(tiers):
    items = []
    for key, (label, size, desc) in SLOTS.items():
        for t in tiers:
            items.append((f"game_background_zone_1_{key}_tier{t+1}.png", key,
                          size, f"{desc}. {TIERS[t]}. {VIEW}"))
    return items


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    ap = argparse.ArgumentParser()
    ap.add_argument("--apply", action="store_true")
    ap.add_argument("--limit", type=int)
    ap.add_argument("--jobs", type=int, default=3)
    ap.add_argument("--tier", help="只跑指定档位，如 1 或 1,2")
    ap.add_argument("--rmbg", action="store_true", help="强制 RMBG 重新抠图")
    a = ap.parse_args()

    tiers = [int(x) - 1 for x in a.tier.split(",")] if a.tier else list(range(4))
    items = build_items(tiers)
    if a.limit:
        items = items[:a.limit]
    os.makedirs(os.path.join(OUT, "objects"), exist_ok=True)

    plan = []
    for name, key, size, prompt in items:
        cmd = [sys.executable, SKILL, "t2i", "--style", "bakery-painterly",
               "-p", prompt, "--name", os.path.splitext(name)[0],
               "-s", pick_api_size(*size), "--bg", "transparent",
               "-o", os.path.join(OUT, "objects")]
        plan.append((name, key, size, cmd))

    print("Zone1 店内物件 · %d 张 → %s\n" % (len(plan), OUT))
    if not a.apply:
        for name, key, size, cmd in plan:
            print("  %-56s %s" % (name, "%dx%d" % size))
        print("\n[dry-run] 加 --apply 执行。预估 $%.1f" % (len(plan) * 0.19))
        return

    fails = []

    def run(item):
        name, key, size, cmd = item
        r = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8",
                           errors="replace", timeout=900)
        return (name, r.returncode == 0,
                (r.stdout or "")[-200:] + (r.stderr or "")[-200:])

    with cf.ThreadPoolExecutor(max_workers=a.jobs) as ex:
        for i, (name, ok, err) in enumerate(ex.map(run, plan), 1):
            print("[%d/%d] %s %s %s" % (i, len(plan), "✅" if ok else "❌", name,
                                        "" if ok else err[:150]))
            if not ok:
                fails.append(name)

    print("\n后处理（透明校验 + 尺寸对齐）…")
    from PIL import Image
    for name, key, size, cmd in plan:
        p = os.path.join(OUT, "objects", name)
        if not os.path.exists(p):
            continue
        im = Image.open(p).convert("RGBA")
        mn = im.getchannel("A").getextrema()[0]
        h = im.getchannel("A").histogram()
        tr = h[0] / sum(h) * 100
        if mn > 250 or a.rmbg:
            rmbg(p)
            im = Image.open(p).convert("RGBA")
            mn = im.getchannel("A").getextrema()[0]
            h = im.getchannel("A").histogram()
            tr = h[0] / sum(h) * 100
            print("   %-56s RMBG 兜底" % name)
        fit_to(p, size[0], size[1])
        print("   %-56s %dx%d 透明%.1f%%" % (name, size[0], size[1], tr))

    print("\n完成。失败 %d 张%s" % (len(fails), ("：" + ", ".join(fails)) if fails else ""))


if __name__ == "__main__":
    main()
