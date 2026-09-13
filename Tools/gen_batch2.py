# -*- coding: utf-8 -*-
"""批次 2：Zone1 场景布景重绘（22 张）

20 张透明物件图（5 种 × 4 档升级）+ 2 张实底背景。
升级逻辑必须保留：破旧 → 修复 → 功能配齐 → 装饰美化。

透明处理：t2i 直接请求透明背景（--bg transparent）；若实测透明丢失，
用 RMBG 兜底（--rmbg）。

用法: python Tools/gen_batch2.py [--apply] [--limit N] [--jobs N] [--rmbg]
"""
import argparse
import concurrent.futures as cf
import os
import subprocess
import sys

SKILL = os.path.expanduser("~/.claude/skills/gpt-image-2/gpt_image_2.py")
RMBG = "D:/claudeWorkbase/models_rmbg/rmbg14.onnx"
ART = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                   "..", "Assets", "Project Files", "Game", "Art")
OUT = "D:/claudeWorkbase/_batch2"
API_SIZES = [(1024, 1024), (1024, 1536), (1536, 1024), (2048, 2048)]

# 5 种物件的四档升级描述
TIERS = {
    "house": [
        "a small humble cottage, worn and shabby: cracked plain walls, a few broken "
        "roof shingles, bare unkempt surroundings, modest and plain",
        "a neat repaired cottage: intact white-cream walls, tidy red roof, small clean "
        "windows with shutters, a welcome mat",
        "a charming upgraded cottage with added decoration: cream walls with warm wood "
        "trim, red tile roof, flower boxes under the windows, a small porch",
        "a beautiful fully decorated storybook cottage: warm cream and wood facade, "
        "red tile roof, lush flower boxes, lanterns, a cozy inviting porch with planters",
    ],
    "table": [
        "a simple worn wooden outdoor table and two plain benches, faded and scuffed "
        "surface, no accessories",
        "a repaired wooden table and benches: light wood surface, clean white metal "
        "frame, freshly painted",
        "an upgraded outdoor table set: table draped with a cream cloth, chairs with "
        "matching soft cushions",
        "a beautifully decorated cafe table set: cream cloth, cushioned chairs, a small "
        "vase of daisies and a glass candle holder on top",
    ],
    "bush": [
        "a tiny sparse sapling bush: thin drooping twigs, only a few new leaves, "
        "patchy grass base",
        "a grown full bush: dense conical evergreen shrub with lush foliage, a few tiny "
        "white flowers on the grass",
        "a topiary bush trimmed into a simple spiral shape, taller, neat and rounded",
        "a tall elaborate spiral topiary with many smooth coils, perfectly trimmed "
        "glossy foliage, elegant and refined",
    ],
    "fence": [
        "a broken old wooden fence: split and leaning planks, missing sections, weeds "
        "growing at the base",
        "a repaired tidy pale natural-wood plank fence with a neat row of small round "
        "shrubs along the base",
        "a completed wooden fence with evenly spaced vintage black wall lanterns and "
        "well-rounded shrubs",
        "a premium cream-painted fence with the same lanterns, perfectly rounded lush "
        "shrubs, immaculate finish",
    ],
    "fountain": [
        "an abandoned dirty stone fountain: cracked grimy masonry, rubbish piled in the "
        "dry basin, no water",
        "a repaired clean stone fountain: intact stonework, emptied basin, still no water",
        "a working fountain with clear water filling the basin, pale beige neat stone, "
        "a ring of low green plants around the base",
        "an elegant running fountain with water flowing from both tiers, bright clean "
        "stone, lush plants with small white flowers around the base",
    ],
}

KIND_LABEL = {"house": "cottage", "table": "outdoor table set",
              "bush": "topiary bush", "fence": "garden fence",
              "fountain": "garden fountain"}

T3D = ("Isometric 45-degree game asset, single object isolated on a fully transparent "
       "background, no ground plane, no scenery, no shadow cast on the background. "
       "Hand-painted casual game illustration style with rich material texture, warm "
       "key light from upper front, soft ambient occlusion, gentle contact shading under "
       "the object. Warm caramel-brown, cream and soft pastel palette. No text, no UI.")


def pick_api_size(w, h):
    ar = w / float(h)

    def matched(s):
        return abs(s[0] / s[1] - ar) <= 0.02

    cands = [s for s in API_SIZES if matched(s)]
    if cands:
        enough = [s for s in cands if s[0] * s[1] >= w * h]
        return "%dx%d" % min(enough or cands, key=lambda s: s[0] * s[1])
    return "%dx%d" % min(API_SIZES, key=lambda s: abs(s[0] / s[1] - ar))


def build_items():
    """生成 22 条任务：name, 相对路径, 尺寸, 提示词, 是否透明"""
    items = []
    # 2 张实底背景
    items.append(("game_backgound_zone_1.png", "Scene/Zone1", (941, 1672),
                  "Vertical cozy garden courtyard game background: soft green lawn, "
                  "warm cream garden walls, gentle trees and hedges blurred in the "
                  "upper area, clean empty lawn in the lower two-thirds reserved for "
                  "gameplay objects. Warm painterly hand-painted lighting. "
                  "No text, no UI, no characters.", False))
    items.append(("game_zone_1_preview.png", "Scene/Zone1", (941, 580),
                  "Horizontal cozy garden courtyard preview: soft green lawn, warm cream "
                  "garden walls, trees and hedges softly blurred in the background, clean "
                  "empty lawn in the center. Warm painterly lighting. "
                  "No text, no UI, no characters.", False))
    # 20 张透明物件
    for kind, label in KIND_LABEL.items():
        for tier in range(4):
            items.append((f"z1_{kind}_{tier+1}.png", "Scene/Zone1", (1024, 1024),
                          f"A {label}, upgrade tier {tier+1}: {TIERS[kind][tier]}. {T3D}",
                          True))
    return items


def rmbg(path):
    """本地 RMBG 兜底抠图（t2i 透明丢失时用）。"""
    try:
        import numpy as np
        import onnxruntime as ort
        from PIL import Image
        sess = ort.InferenceSession(RMBG, providers=["CPUExecutionProvider"])
        name = sess.get_inputs()[0].name
        orig = Image.open(path).convert("RGBA")
        flat = Image.new("RGB", orig.size, (255, 255, 255))
        flat.paste(orig, (0, 0), orig)
        flat = flat.resize((1024, 1024), Image.LANCZOS)
        arr = np.asarray(flat).astype(np.float32) / 255.0 - 0.5
        m = sess.run(None, {name: arr.transpose(2, 0, 1)[None]})[0][0, 0]
        m = np.clip(m, 0, 1)
        m = (m - m.min()) / (m.max() - m.min() + 1e-8)
        mi = Image.fromarray((m * 255).astype(np.uint8)).resize(orig.size, Image.LANCZOS)
        orig.putalpha(mi)
        orig.save(path)
        return True
    except Exception as e:
        print("      RMBG 失败:", str(e)[:120])
        return False


def fit_to(path, w, h, keep_alpha):
    from PIL import Image
    im = Image.open(path).convert("RGBA")
    if im.size == (w, h):
        return
    src_ar, dst_ar = im.width / im.height, w / float(h)
    if abs(src_ar - dst_ar) < 1e-3:
        im = im.resize((w, h), Image.LANCZOS)
    else:
        scale = min(w / im.width, h / im.height)
        nw, nh = max(1, round(im.width * scale)), max(1, round(im.height * scale))
        cv = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        cv.paste(im.resize((nw, nh), Image.LANCZOS),
                 ((w - nw) // 2, (h - nh) // 2))
        im = cv
    im.save(path) if keep_alpha else im.convert("RGB").save(path)


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    ap = argparse.ArgumentParser()
    ap.add_argument("--apply", action="store_true")
    ap.add_argument("--limit", type=int)
    ap.add_argument("--jobs", type=int, default=4)
    ap.add_argument("--rmbg", action="store_true",
                    help="生成后用本地 RMBG 强制重新抠图")
    a = ap.parse_args()

    items = build_items()
    if a.limit:
        items = items[:a.limit]
    os.makedirs(OUT, exist_ok=True)

    plan = []
    for name, sub, size, prompt, transparent in items:
        src = os.path.join(ART, sub, name)
        cmd = [sys.executable, SKILL, "t2i", "--style", "bakery-painterly",
               "-p", prompt, "--name", os.path.splitext(name)[0],
               "-s", pick_api_size(*size),
               "--bg", "transparent" if transparent else "opaque",
               "-o", os.path.join(OUT, sub)]
        plan.append((name, src, size, cmd, transparent))

    print("批次 2 · Zone1 场景布景 · %d 张 → %s\n" % (len(plan), OUT))
    if not a.apply:
        for name, src, size, cmd, tr in plan:
            print("  %-34s %-12s %s" % (name, "%dx%d" % size,
                                        "透明" if tr else "实底"))
        print("\n[dry-run] 加 --apply 执行。预估 $%.1f" % (len(plan) * 0.19))
        return

    fails = []

    def run(item):
        name, src, size, cmd, tr = item
        r = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8",
                           errors="replace", timeout=900)
        return (name, r.returncode == 0,
                (r.stdout or "")[-200:] + (r.stderr or "")[-200:])

    with cf.ThreadPoolExecutor(max_workers=a.jobs) as ex:
        for i, (name, ok, err) in enumerate(ex.map(run, plan), 1):
            print("[%d/%d] %s %s %s" % (i, len(plan), "✅" if ok else "❌", name,
                                        "" if ok else err[:160]))
            if not ok:
                fails.append(name)

    # 后处理：尺寸对齐 + 透明校验/兜底
    print("\n后处理（尺寸对齐 + 透明校验）…")
    from PIL import Image
    for name, src, size, cmd, tr in plan:
        p = os.path.join(OUT, "Scene/Zone1", os.path.splitext(name)[0] + ".png")
        if not os.path.exists(p):
            continue
        if tr:
            al = Image.open(p).convert("RGBA").getchannel("A")
            mn, _ = al.getextrema()
            if mn > 250 or a.rmbg:            # 透明丢失 → RMBG 兜底
                why = "强制" if a.rmbg else "透明丢失"
                okr = rmbg(p)
                mn2 = Image.open(p).convert("RGBA").getchannel("A").getextrema()[0]
                print("   %-34s %s → RMBG%s (alpha_min %d→%d)"
                      % (name, why, "" if okr else "失败", mn, mn2))
        fit_to(p, size[0], size[1], tr)
        print("   %-34s → %dx%d %s" % (name, size[0], size[1], "透明" if tr else "实底"))

    print("\n完成。失败 %d 张%s" % (len(fails), ("：" + ", ".join(fails)) if fails else ""))


if __name__ == "__main__":
    main()
