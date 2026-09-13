# -*- coding: utf-8 -*-
"""批次 1：背景板重绘（11 张）

用 t2i + bakery-painterly 风格重新生成，保持原图长宽比与构图主题。
API 尺寸受限于 1024²/1024x1536/1536x1024/2048²，非标准比例走本地等比缩放。

用法: python Tools/gen_batch1.py [--apply] [--limit N] [--jobs N]
      default = dry-run，只打印将执行的命令
"""
import argparse
import os
import subprocess
import sys

SKILL = os.path.expanduser("~/.claude/skills/gpt-image-2/gpt_image_2.py")
ART = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                   "..", "Assets", "Project Files", "Game", "Art")
OUT = "D:/claudeWorkbase/_batch1"

# 每张图：源文件名 → (相对 Art 的路径, 尺寸, 内容提示词)
ITEMS = [
    ("back_1.png", "Scene/Backgrounds", (2048, 2048),
     "Spring dessert meadow level background: rolling bright green grass hills, "
     "pastel macaron-shaped mountains in the far distance, a large frosting-topped "
     "gingerbread cottage on the left mid-ground, round-headed green trees, soft cream "
     "sky with cotton clouds. The bottom quarter is a clean flat empty grass slope "
     "reserved for the game board. No text, no UI, no characters."),
    ("back_2.png", "Scene/Backgrounds", (2048, 2048),
     "Warm autumn countryside level background: soft rolling warm-yellow hills, cream "
     "winding path, one slim green tree and two yellow-leaved autumn trees, scattered "
     "brown stones, a tiny red-roofed cottage on the left slope, warm goose-yellow sky "
     "with cotton clouds. The bottom quarter is a clean flat empty warm-yellow slope "
     "reserved for the game board. No text, no UI, no characters."),
    ("back_3.png", "Scene/Backgrounds", (2048, 2048),
     "Winter snow village level background with warm light: soft snowy rolling hills, "
     "a light-yellow-edged winding snow road, scattered brown stones, a cozy little "
     "shop with a snow-capped roof and glowing windows on the left slope, two conical "
     "snow-covered trees, pale cream sky with cotton clouds. Leave a clean flat snow "
     "area along the bottom for the game board. No text, no UI, no characters."),
    ("back_4.png", "Scene/Backgrounds", (2048, 2048),
     "Sunset spring countryside level background: warm orange-to-pink gradient sunset "
     "sky, cotton clouds, soft hills with reddish-brown accents, fresh green grass "
     "slopes, a pale earth-yellow winding path, one round green tree and two pink-"
     "blossom trees, a red-tiled cottage with glowing windows and smoke on the left "
     "slope. The bottom fifth is a clean flat empty grass slope reserved for the game "
     "board. No text, no UI, no characters."),
    ("game_backgound_zone_2.png", "Scene/Zone2", (941, 1672),
     "Vertical cozy bakery shop interior background for a mobile game: warm cream "
     "walls, polished wooden floor, shelves with cakes and pastries softly blurred in "
     "the upper area, a large clean empty floor space in the lower two-thirds reserved "
     "for gameplay objects. Warm painterly lighting. No text, no UI, no characters."),
    ("game_zone_2_preview.png", "Scene/Zone2", (941, 580),
     "Horizontal cozy bakery shop interior preview: warm cream walls, polished wooden "
     "floor, shelves with cakes softly blurred in the background, clean empty floor "
     "space in the center. Warm painterly lighting. No text, no UI, no characters."),
    ("game_zone_3_preview.png", "Scene/Zone3", (941, 580),
     "Horizontal grand patisserie interior preview: elegant warm gold and cream decor, "
     "marble floor, chandeliers, glass display cases with cakes softly blurred, clean "
     "empty floor space in the center. Warm painterly lighting. No text, no UI, "
     "no characters."),
    ("game_background_zone_1_full_tier1.png", "_Pending/Zone1Shop/full", (768, 1344),
     "Vertical starter bakery shop interior, tier 1 (simple and modest): plain cream "
     "walls, light wooden floor, a simple L-shaped counter, basic open shelves with a "
     "few cakes. Clean empty lower area for gameplay objects. Warm painterly lighting. "
     "No text, no UI, no characters."),
    ("game_background_zone_1_full_tier2.png", "_Pending/Zone1Shop/full", (768, 1344),
     "Vertical cozy local bakery interior, tier 2 (nicer): warm wooden wall panels, "
     "wooden floor with grain, an L-shaped wooden counter with carved details, wooden "
     "shelves with cakes, framed pictures on the wall. Clean empty lower area for "
     "gameplay objects. Warm painterly lighting. No text, no UI, no characters."),
    ("game_background_zone_1_full_tier3.png", "_Pending/Zone1Shop/full", (768, 1344),
     "Vertical premium bakery boutique interior, tier 3 (upscale): cream walls with "
     "subtle gold crown wallpaper pattern, wooden floor, wide wooden counter, tall "
     "carved display cabinet, a curved glass refrigerated display case, two fabric "
     "pendant lamps. Clean empty lower area for gameplay objects. Warm painterly "
     "lighting. No text, no UI, no characters."),
    ("game_background_zone_1_full_tier4.png", "_Pending/Zone1Shop/full", (768, 1344),
     "Vertical grand patisserie interior, tier 4 (luxurious French style): ornate "
     "cream and gold walls with mural paintings, marble floor, chandeliers, long "
     "glass display cases full of elegant cakes, gold-framed mirrors. Clean empty "
     "lower area for gameplay objects. Warm painterly lighting. No text, no UI, "
     "no characters."),
]

API_SIZES = [(1024, 1024), (1024, 1536), (1536, 1024), (2048, 2048)]


def pick_api_size(w, h):
    """按长宽比挑 API 尺寸；同比例时选**不低于原图**的最小档位，避免白丢分辨率。

    例：2048x2048 → 2048x2048（而非 1024x1024）；768x1344 → 1024x1536。
    """
    ar = w / float(h)

    def matched(s):
        return abs(s[0] / s[1] - ar) <= 0.02

    cands = [s for s in API_SIZES if matched(s)]
    if cands:
        enough = [s for s in cands if s[0] * s[1] >= w * h]   # 不低于原图
        pool = enough or cands                                 # 都没有就用最大的
        return "%dx%d" % min(pool, key=lambda s: s[0] * s[1])

    return "%dx%d" % min(API_SIZES, key=lambda s: abs(s[0] / s[1] - ar))


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    ap = argparse.ArgumentParser()
    ap.add_argument("--apply", action="store_true", help="真正执行（默认 dry-run）")
    ap.add_argument("--limit", type=int)
    ap.add_argument("--jobs", type=int, default=4)
    a = ap.parse_args()

    items = ITEMS[:a.limit] if a.limit else ITEMS
    os.makedirs(OUT, exist_ok=True)

    plan = []
    for name, sub, size, prompt in items:
        src = os.path.join(ART, sub, name)
        api = pick_api_size(*size)
        cmd = [sys.executable, SKILL, "t2i", "--style", "bakery-painterly",
               "-p", prompt, "--name", os.path.splitext(name)[0],
               "-s", api, "--bg", "opaque", "-o", os.path.join(OUT, sub)]
        plan.append((name, src, size, api, cmd))

    print("批次 1 · 背景板 · %d 张 → %s\n" % (len(plan), OUT))
    for name, src, size, api, cmd in plan:
        ok = "✓" if os.path.exists(src) else "✗源文件缺失"
        print("  %-42s 原%s → API %s  %s" % (name, "%dx%d" % size, api, ok))

    if not a.apply:
        print("\n[dry-run] 加 --apply 执行。预估 $%.1f" % (len(plan) * 0.19))
        return

    # 并发执行（每张一个 CLI 进程，天然隔离）
    import concurrent.futures as cf
    fails = []

    def run(item):
        name, src, size, api, cmd = item
        r = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8",
                           errors="replace", timeout=900)
        if r.returncode != 0:
            return (name, False, (r.stdout or "")[-300:] + (r.stderr or "")[-300:])
        return (name, True, "")

    with cf.ThreadPoolExecutor(max_workers=a.jobs) as ex:
        for i, (name, ok, err) in enumerate(ex.map(run, plan), 1):
            print("[%d/%d] %s %s %s" % (i, len(plan), "✅" if ok else "❌", name,
                                        "" if ok else err[:200]))
            if not ok:
                fails.append(name)

    # 尺寸对齐：等比缩放，不拉伸
    print("\n对齐原图尺寸…")
    from PIL import Image
    for name, src, size, api, cmd in plan:
        out = os.path.join(OUT, os.path.dirname(src) and "", os.path.splitext(name)[0] + ".png")
        # OUT 下按 sub 目录组织
        sub = [s for n, s, _, _, _ in plan if n == name]
        cand = None
        for root, dirs, files in os.walk(OUT):
            if os.path.splitext(name)[0] + ".png" in files:
                cand = os.path.join(root, os.path.splitext(name)[0] + ".png")
        if not cand or not os.path.exists(cand):
            continue
        im = Image.open(cand).convert("RGBA")
        if im.size == size:
            continue
        src_ar, dst_ar = im.width / im.height, size[0] / float(size[1])
        if abs(src_ar - dst_ar) < 1e-3:
            im.resize(size, Image.LANCZOS).convert("RGB").save(cand)
        else:
            scale = min(size[0] / im.width, size[1] / im.height)
            nw, nh = round(im.width * scale), round(im.height * scale)
            cv = Image.new("RGBA", size, (0, 0, 0, 0))
            cv.paste(im.resize((nw, nh), Image.LANCZOS), ((size[0] - nw) // 2,
                                                          (size[1] - nh) // 2))
            cv.convert("RGB").save(cand)
        print("   %-42s → %dx%d" % (name, size[0], size[1]))

    print("\n完成。失败 %d 张%s" % (len(fails), ("：" + ", ".join(fails)) if fails else ""))


if __name__ == "__main__":
    main()
