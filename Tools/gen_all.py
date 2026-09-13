# -*- coding: utf-8 -*-
"""全量重绘 Art/ 在用资源（t2i + bakery-painterly 风格）。

策略：输出到独立目录（不覆盖原文件），保持同名 + 同尺寸 + 同透明属性，
     满意后再由 migrate 脚本对位替换。

提示词来源：按分组使用「物件类型 + 档位 + 统一视角/背景约束」模板。
对于文件名可识别的资源（pieces/tiles/chests 等）自动推导描述。

用法:
  python Tools/gen_all.py --list                    # 列出计划
  python Tools/gen_all.py --group Gameplay/Pieces --apply
  python Tools/gen_all.py --all --apply --jobs 4
"""
import argparse
import concurrent.futures as cf
import json
import os
import re
import subprocess
import sys
import collections

from PIL import Image

SKILL = os.path.expanduser("~/.claude/skills/gpt-image-2/gpt_image_2.py")
HERE = os.path.dirname(os.path.abspath(__file__))
OUT_ROOT = "D:/claudeWorkbase/_redraw"
API_SIZES = [(1024, 1024), (1024, 1536), (1536, 1024), (2048, 2048)]

# 统一约束
CUTOUT = ("Isolated cutout on a completely transparent background: absolutely NO ground "
          "plane, NO floor, NO grass, NO backdrop, NO cast shadow, NO glow, NO vignette. "
          "Centered, clean silhouette, hand-painted casual game illustration with rich "
          "material texture, warm key light from upper front, soft ambient occlusion. "
          "Warm caramel-brown, cream and soft pastel palette. No text, no UI, no letters.")
SCENE = ("Hand-painted casual game illustration background, rich material texture, warm "
         "key light from upper front, warm caramel-brown, cream and soft pastel palette. "
         "No text, no UI, no letters, no characters.")

# 分组 → (是否透明, 描述模板)
GROUPS = {
    # 棋子是同族合成链（1→N 逐级升级），必须按序号描述具体物品
    "Gameplay/Pieces": (True,
        "A match-3 game piece showing {item}. Thick volumetric 3D-rendered dessert form, "
        "matte fondant and cream material, appetizing and cute, slightly angled top-down "
        "view, single item centered. IMPORTANT: render EXACTLY the item described and "
        "nothing more - do not upgrade it into a fancier dessert. Early plain items must "
        "stay plain and simple; do not add berries, cream swirls, sprinkles or extra "
        "decorations that are not literally part of the description."),
    "Gameplay/Tiles": (True,
        "A match-3 board tile background: {desc}. Top-down orthographic rounded square "
        "surface, flat clean empty center reserved for an icon, soft rounded corners, "
        "subtle inner bevel only, NO upward-flipped lace border, NO ornament in center."),
    "Gameplay/Tiles/Special": (True,
        "A match-3 special mechanic tile: {desc}. Top-down orthographic rounded square, "
        "clear readable symbolic visual, flat center."),
    # 宝箱必须保持"闭合"状态（玩家点击开启的对象），且 1→5 档逐级华丽
    "Gameplay/Chests": (True,
        "A game treasure chest with its LID CLOSED: {desc}. Isometric 45-degree angle, "
        "rich wood and gilded metal materials, padlock and decorative bands, LID FIRMLY "
        "SHUT, no coins or contents visible outside. Ornate detail increases with the "
        "tier number: tier 1 plain and simple, tier 3 added star emblem, tier 5 highly "
        "ornate with gems and crown motifs."),
    "Gameplay/Characters": (True,
        "A cute Q-version game character portrait, 3-head-tall chibi proportion: {desc}."),
    "Gameplay/PowerUps": (True,
        "A game power-up icon: {desc}. Simple bold readable symbol, centered."),
    "Gameplay/Dock": (True,
        "A match-3 collection dock tray: {desc}. Flat front view, wooden and cream "
        "material, clean empty slots."),
    "Gameplay/Misc": (True, "A casual game asset: {desc}. Simple bold readable form."),
    "UI/Icons": (True,
        "A mobile game UI icon: {desc}. Simple bold readable silhouette, centered, flat "
        "matte fill with subtle diffuse shading, clean edges."),
    "UI/Common": (True,
        "A mobile game UI element: {desc}. Clean rounded shape, cream and warm tones. "
        "If it is a panel or button base, keep the edges uniform and stretch-friendly, "
        "flat center."),
    "UI/Store": (True, "A mobile game store UI element: {desc}. Clean rounded shape, "
                       "warm cream and gold accents."),
    "UI/Tutorial": (True, "A mobile game tutorial asset: {desc}. Simple bold readable form."),
    "Dialog": (True, "A visual-novel dialog UI element: {desc}. Clean rounded shape, "
                     "warm cream tones."),
    "Fx": (True, "A game visual effect sprite: {desc}. Centered, soft edges, glow-free, "
                 "matte finish."),
    "Map": (False, "A casual game level map element: {desc}. " + SCENE),
    "Scene/Zone1": (True, "A cozy bakery shop interior prop: {desc}. Flat front elevation "
                          "view, eye-level straight-on, symmetrical, centered."),
    "Fonts/FredokaOne 120": (True, "An ornate decorative game title text texture plate, "
                                   "blank with no letters, warm cream and gold."),
    "Fonts/FredokaOne 50": (True, "An ornate decorative game text texture plate, blank "
                                  "with no letters, warm cream and gold."),
}


# 棋子合成链：族名 → 按序号(1起)的物品描述
PIECE_CHAINS = {
    "cake": ["a small paper sack of flour", "a bowl of pale cake batter",
             "a flat round shortbread disc", "a round chocolate-chip cookie",
             "a plain vanilla cupcake in a pleated liner",
             "a blueberry muffin with berries on top",
             "a strawberry tart with sliced strawberries in a pastry shell",
             "a square cherry-topped cheesecake slice",
             "a layered tiramisu square with cocoa dusting",
             "a slice of layered sponge layer cake with a strawberry on top",
             "a triangular slice of berry cheesecake",
             "a tall glass dessert cup layered with cream, berries and a mint leaf"],
    "coffe": ["three roasted coffee beans", "a small conical pile of coffee grounds",
              "a plain white paper takeaway cup", "a paper takeaway cup with an orange sleeve",
              "an orange ceramic coffee mug", "a white ceramic coffee cup on a saucer",
              "a white cup of latte with a frothy top",
              "a light blue cup of cappuccino with latte art",
              "a light blue cup with a sleeping cat face drawn in foam",
              "a glass of iced coffee with ice cubes and mint",
              "a glass of layered affogato with coffee and cream",
              "a tall iced coffee glass with cream, berries and a paper parasol"],
    "candy": ["a single round purple gumdrop", "a wrapped blue candy with twisted ends",
              "a pink-and-white swirl lollipop on a stick",
              "a glass jar packed with colourful candy balls",
              "a small purple gift box overflowing with colourful candies"],
    "soda": ["two pale blue ice cubes", "a bare clear glass bottle",
             "a dark cola glass bottle with a red label",
             "an open cola bottle with its red cap beside it",
             "a dark cola bottle with a glass of cola and ice beside it"],
    "kettle": ["a simple terracotta clay teapot with a round body",
               "a glossy bright red ceramic teapot",
               "a tall slender red ceramic pouring pot with a long spout",
               "a silver metal stove-top whistling kettle with a black knob",
               "a silver metal pour-over gooseneck kettle",
               "a glass drip coffee carafe with a black plastic handle and lid, "
               "dark coffee inside",
               "an electric kettle in red and yellow plastic with a water window "
               "and a power switch",
               "a beige semi-automatic espresso machine with a portafilter and a "
               "cup under the spout",
               "an upscale beige espresso machine machine with two portafilters and "
               "two cups, pressure gauge on the front"],
    "mixer": ["a green-handled measuring scoop filled with pale batter",
              "a green-handled wire whisk",
              "an empty ceramic mixing bowl",
              "a ceramic mixing bowl with a green-handled whisk resting inside",
              "a green handheld electric stick blender",
              "a mint-green handheld electric hand mixer with beaters",
              "a mint-green electric hand mixer resting in a mixing bowl",
              "a mint-green stand mixer with a stainless bowl and a dough hook",
              "a mint-green stand mixer with a stainless bowl and a wire whisk "
              "attachment"],
    "placeholder": ["a small cafe storefront building icon"],
    "coin": ["a round gold coin with a star emblem"],
    "energy": ["a blue lightning bolt energy symbol"],
    "gem": ["a purple gem crystal"], "candle": ["a lit candle"], "croissant": ["a croissant"],
    "donut": ["a glazed donut"], "soda_can": ["a soda can"],
}


def piece_item(fname):
    """棋子文件名 → 该链第几级的物品描述。"""
    base = os.path.basename(fname)
    m = re.match(r"game_([a-z_]+?)_?(\d+)?\.png$", base)
    if not m:
        return None
    fam, idx = m.group(1), m.group(2)
    chain_list = PIECE_CHAINS.get(fam)
    if not chain_list:
        return None
    i = int(idx) - 1 if idx else 0
    if 0 <= i < len(chain_list):
        return chain_list[i]
    return chain_list[-1]


def norm(p):
    return os.path.abspath(p).replace("\\", "/").lower()


def load_used():
    refs = json.load(open(os.path.join(HERE, "art_refs.json"), encoding="utf-8"))
    return {norm(p) for p in refs["used"]}


def scan_group(group):
    sys.path.insert(0, HERE)
    import art_batch_plan as P
    g = P.scan()
    out = []
    for k, fs in g.items():
        if k == group:
            out += fs
    return sorted(out)


def humanize(fname):
    """文件名 → 可读描述（去掉前缀与扩展名）。"""
    n = os.path.splitext(os.path.basename(fname))[0]
    n = re.sub(r"^(ui_|game_|z\d_|sprite_)", "", n)
    return n.replace("_", " ").strip()


def pick_api_size(w, h):
    ar = w / float(h)

    def matched(s):
        return abs(s[0] / s[1] - ar) <= 0.02

    c = [s for s in API_SIZES if matched(s)]
    if c:
        e = [s for s in c if s[0] * s[1] >= w * h]
        return "%dx%d" % min(e or c, key=lambda s: s[0] * s[1])
    return "%dx%d" % min(API_SIZES, key=lambda s: abs(s[0] / s[1] - ar))


def has_alpha(p):
    try:
        im = Image.open(p)
        if im.mode not in ("RGBA", "LA", "P"):
            return False
        return im.convert("RGBA").getchannel("A").getextrema()[0] < 250
    except Exception:
        return False


def build_plan(groups, limit=None, skip_existing=False):
    used = load_used()
    items = []
    for grp in groups:
        if grp not in GROUPS:
            print("跳过未知分组:", grp)
            continue
        transparent_default, tpl = GROUPS[grp]
        for f in scan_group(grp):
            if norm(f) not in used:
                continue                       # 孤儿资源不重绘
            im = Image.open(f)
            w, h = im.size
            tr = has_alpha(f) or transparent_default
            desc = humanize(f)
            if grp == "Gameplay/Pieces":
                item = piece_item(f)
                if item:
                    prompt = tpl.format(item=item) + " " + (CUTOUT if tr else SCENE)
                else:
                    prompt = tpl.format(item=desc) + " " + (CUTOUT if tr else SCENE)
            else:
                prompt = tpl.format(desc=desc) + " " + (CUTOUT if tr else SCENE)
            outdir = os.path.join(OUT_ROOT, grp)
            if skip_existing and os.path.exists(os.path.join(outdir, os.path.basename(f))):
                continue
            items.append({
                "group": grp, "src": f, "name": os.path.basename(f),
                "size": (w, h), "transparent": tr, "prompt": prompt,
                "out": os.path.join(OUT_ROOT, grp),
            })
    if limit:
        items = items[:limit]
    return items


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    ap = argparse.ArgumentParser()
    ap.add_argument("--group", action="append", help="只处理指定分组，可重复")
    ap.add_argument("--all", action="store_true", help="所有在用分组")
    ap.add_argument("--list", action="store_true")
    ap.add_argument("--apply", action="store_true")
    ap.add_argument("--exclude", action="append", default=[],
                    help="排除分组，可重复。如 --exclude Gameplay/Pieces")
    ap.add_argument("--skip-existing", action="store_true",
                    help="输出目录已有同名文件则跳过（避免重复烧钱）")
    ap.add_argument("--limit", type=int)
    ap.add_argument("--jobs", type=int, default=3,
                    help="并发数。注意：账号低余额时并发上限会降到 3，超出会被直接拒绝")
    a = ap.parse_args()

    sys.path.insert(0, HERE)
    import art_batch_plan as P
    all_groups = sorted({k for k in P.scan()} & set(GROUPS))
    all_groups = [g for g in all_groups if g not in set(a.exclude or [])]
    groups = a.group or (all_groups if a.all else [])
    if not groups:
        sys.exit("需指定 --group 或 --all（用 --list 查看）")

    plan = build_plan(groups, a.limit, a.skip_existing)
    if not plan:
        sys.exit("没有匹配的在用资源")

    by = collections.Counter(i["group"] for i in plan)
    print("计划重绘 %d 张，预估 $%.0f\n" % (len(plan), len(plan) * 0.19))
    for g, n in sorted(by.items()):
        print("   %-28s %3d 张" % (g, n))
    if a.list or not a.apply:
        for i in plan[:40]:
            print("     %-30s %-30s %s" % (i["group"], i["name"],
                                           "%dx%d" % i["size"]))
        if len(plan) > 40:
            print("     … 其余 %d 张" % (len(plan) - 40))
        if not a.apply:
            print("\n[dry-run] 加 --apply 执行")
        return

    for d in {i["out"] for i in plan}:
        os.makedirs(d, exist_ok=True)

    cmds = []
    for i in plan:
        c = [sys.executable, SKILL, "t2i", "--style", "bakery-painterly",
             "-p", i["prompt"], "--name", os.path.splitext(i["name"])[0],
             "-s", pick_api_size(*i["size"]),
             "--bg", "transparent" if i["transparent"] else "opaque",
             "-o", i["out"]]
        cmds.append((i, c))

    ok = fail = 0
    errors = []

    import time as _t

    def run(pair):
        """执行单张；并发限流/网络类失败自动退避重试（这类失败重跑即成）。

        低余额账号并发上限会降到 3，超出直接拒绝——必须重试而非当失败丢弃。
        """
        i, c = pair
        last = ""
        for attempt in range(4):
            r = subprocess.run(c, capture_output=True, text=True, encoding="utf-8",
                               errors="replace", timeout=900)
            if r.returncode == 0:
                return (i, True, "")
            last = (r.stdout or "")[-200:] + (r.stderr or "")[-200:]
            low = last.lower()
            if not any(k in low for k in ("too many concurrent", "rate limit",
                                          "precharge", "timeout", "502", "503")):
                break
            _t.sleep(min(2 ** attempt * 5, 30))
        return (i, False, last)

    def run_all(pairs, label=""):
        """并发跑一批，返回 (成功数, 失败数, 失败清单)。"""
        okn = fln = 0
        errs = []
        with cf.ThreadPoolExecutor(max_workers=max(1, a.jobs)) as ex:
            for n, (i, good, err) in enumerate(ex.map(run, pairs), 1):
                if good:
                    okn += 1
                else:
                    fln += 1
                    errs.append((i["name"], err))
                print("%s[%d/%d] %s %s" % (label, n, len(pairs),
                                           "✅" if good else "❌", i["name"]))
                if not good:
                    print("        ", err[:150])
        return okn, fln, errs

    n_ok, n_fail, errors = run_all(cmds)
    ok += n_ok
    fail += n_fail

    # 失败补跑：限流类失败重跑即成功
    if errors:
        retry_pairs = [(i, c) for i, c in cmds
                       if i["name"] in {e[0] for e in errors}]
        print("\n补跑 %d 张失败的…" % len(retry_pairs))
        r_ok, r_fail, errors = run_all(retry_pairs, label="补")
        ok += r_ok - (len(retry_pairs) - r_ok)
        ok = n_ok + r_ok
        fail = len(plan) - ok

    # 尺寸对齐
    print("\n尺寸对齐…")
    for i in plan:
        p = os.path.join(i["out"], i["name"])
        if not os.path.exists(p):
            continue
        im = Image.open(p).convert("RGBA")
        w, h = i["size"]
        if im.size != (w, h):
            src_ar, dst_ar = im.width / im.height, w / float(h)
            if abs(src_ar - dst_ar) < 1e-3:
                im = im.resize((w, h), Image.LANCZOS)
            else:
                s = min(w / im.width, h / im.height)
                nw, nh = max(1, round(im.width * s)), max(1, round(im.height * s))
                cv = Image.new("RGBA", (w, h), (0, 0, 0, 0))
                cv.paste(im.resize((nw, nh), Image.LANCZOS),
                         ((w - nw) // 2, (h - nh) // 2))
                im = cv
        im.save(p) if i["transparent"] else im.convert("RGB").save(p)

    print("\n完成：成功 %d，失败 %d" % (ok, fail))
    if errors:
        print("\n失败清单：")
        for n, e in errors[:20]:
            print("   %-40s %s" % (n, e[:120]))


if __name__ == "__main__":
    main()
