# -*- coding: utf-8 -*-
"""三场景室内店铺：5 槽位 × 4 档可升级部件（Zone1/2/3）。

设计依据: docs/三场景室内店铺设计方案.md

核心工艺 —— **链式升级**：
  同一槽位的 4 档**必须形态连贯**（同一个柜子逐步变好），
  所以不能 4 档各自 t2i 独立生成（会得到 4 个不同的柜子）。
  做法：t1 用 t2i 从零生成 → t2 以 t1 为参考图 edit 升级 → t3 以 t2 为参考 → t4 以 t3 为参考。
  这样每档都严格是"上一档的加强版"。

尺寸工艺：
  所有部件统一输出 **1024x1024 透明底方图**，内容按最长边缩到 96% 居中。
  理由：prefab 用 preserveAspect 显示，方图 ⇒ 视觉大小完全由 prefab 的 rect 尺寸决定，
  15 个槽位的相对大小关系可精确设计（见设计方案第三节）。
  若按内容尺寸出图，小凳与大柜会看起来一样大。

用法:
  python Tools/gen_zone_items.py                        # dry-run
  python Tools/gen_zone_items.py --apply                # 全跑（60 张）
  python Tools/gen_zone_items.py --apply --only z1      # 只做 Zone1
  python Tools/gen_zone_items.py --apply --only z1:building5 --tiers 4
"""
import argparse
import os
import subprocess
import sys
import time
from concurrent.futures import ThreadPoolExecutor

from PIL import Image

SKILL = os.path.expanduser("~/.claude/skills/gpt-image-2/gpt_image_2.py")
HERE = os.path.dirname(os.path.abspath(__file__))
PROJ = os.path.normpath(os.path.join(HERE, ".."))
ART = os.path.join(PROJ, "Assets", "Project Files", "Game", "Art", "Scene")
OUT = "D:/claudeWorkbase/_zone_items"
OG = (1024, 1024)

# 统一风格：与背景同源（bakery-painterly），强调圆角、低饱和、物件为主
STYLE = (
    "Single isolated game asset of ONE piece of shop furniture, straight-on front view, "
    "perfectly centered, filling most of the frame. "
    "Hand-painted casual mobile game illustration: believable material texture, warm key "
    "light from the upper front, soft ambient occlusion in the crevices, gentle contact "
    "shadow at the very bottom edge, subtle rim light on the top edge. "
    "Rounded friendly shapes, clean readable silhouette, thin warm-brown outline, "
    "matte finish. Muted low-saturation warm palette (cream, light oak, butter, caramel, "
    "soft sage, blush pink). NO text, no letters, no numbers, no logo. "
    "NOT luxurious, no marble, no chandelier, no heavy gold frame, no gemstones. "
    "Fully transparent background, nothing behind or around the object."
)

# 档位递进语义（统一口径）
TIER_NOTE = ("Upgrade level %d of 4: the SAME object as the reference, strictly keeping "
             "its overall shape, proportions, viewing angle and footprint - only the "
             "craftsmanship, materials, surface finish and how generously it is stocked "
             "improve. Nothing is added around or behind it. Still one isolated object on "
             "a fully transparent background.")

ZONES = {
    "z1": dict(
        prefix="z1shop",
        art=os.path.join(ART, "Zone1Shop"),
        label="Zone1 温馨街角面包房",
        context=("A cozy little corner BAKERY. Materials: cream-painted wood, light oak, "
                 "coarse linen, terracotta, simple black metal. Warm afternoon daylight."),
        slots=[
            dict(m="1", sem="counter", cn="面包展示柜",
                 core="a wooden bakery display cabinet for bread and pastries",
                 t=["shabby old wooden cabinet with peeling pale paint, ONE single shelf, "
                    "holding only two plain bread loaves and one croissant, worn and humble",
                    "the same cabinet repainted clean cream white, TWO shelves, neatly "
                    "filled with assorted breads, croissants and round buns",
                    "the same cabinet now with sliding glass doors and a warm strip light "
                    "inside, THREE shelves fully stocked with breads, pastries and one cake",
                    "the same cabinet as a premium piece with gently carved edge trim and a "
                    "soft champagne-gold thin outline, THREE shelves packed with elaborate "
                    "cakes, tarts and macarons, warm interior glow"]),
            dict(m="2", sem="shelf", cn="高储物架",
                 core="a tall wooden storage shelf unit for a bakery",
                 t=["simple three-tier raw unfinished wood shelf with a few scattered empty "
                    "jars and one small sack",
                    "the same shelf neatly painted cream white, FOUR tiers, tidy rows of "
                    "flour jars, linen-lined baskets and folded sacks",
                    "the same shelf with small chalkboard labels on every tier, woven "
                    "baskets, ceramic canisters and one trailing plant on top",
                    "the same shelf as a sturdy FIVE-tier oak unit, a matching ceramic "
                    "canister set, wicker baskets, brass labels, generously stocked"]),
            dict(m="3", sem="table", cn="小圆桌+两椅",
                 core="a small round bakery cafe table with two chairs",
                 t=["a plain square wooden table with a single simple stool",
                    "the same set replaced by a round wooden table with two matching plain "
                    "wooden chairs, bare surface, nothing on the table",
                    "the same round table with a gingham tablecloth, a small vase of "
                    "daisies and two chairs with seat cushions",
                    "the same set upgraded with a marble-look top, softly outlined chairs "
                    "with padded seats, a generous bouquet and a small tiered dessert stand "
                    "on the table"]),
            dict(m="4", sem="bar", cn="咖啡机吧台",
                 core="a small bakery coffee bar counter",
                 t=["a rough little wooden side table with a single pour-over kettle on it",
                    "the same spot becomes a simple wooden counter with a basic home coffee "
                    "machine and two mugs",
                    "the same counter with a proper espresso machine, a coffee grinder, a "
                    "rack of mugs and a small warm lamp",
                    "the same counter as a full professional espresso setup with polished "
                    "copper pipes, two grinders, a full mug rack and a warm overhead lamp"]),
            dict(m="5", sem="tiered_stand", cn="三层点心塔（标志物）",
                 core=("an elegant three-tiered bakery dessert display stand - the signature "
                       "centre-piece of the shop"),
                 t=["a small single-tier wooden plate with only two plain buns on it",
                    "the same piece becomes a two-tier wooden stand with six pastries and "
                    "one small cake",
                    "the same stand as a proper THREE-tier stand with every tier filled "
                    "with breads, pastries and a cake, a glass cloche over the top",
                    "the same stand as an ornate three-tier cake stand, every tier heaped "
                    "with cakes, macarons and fruit tarts, finished with a small floral "
                    "arrangement on top"]),
        ]),
    "z2": dict(
        prefix="z2shop",
        art=os.path.join(ART, "Zone2Shop"),
        label="Zone2 清晨咖啡吧",
        context=("A calm modern NORTHERN-EUROPEAN MINIMALIST COFFEE BAR. Materials: warm "
                 "white painted wood, pale ash wood, muted sage-green paint, slim brass "
                 "details, grey terrazzo. Clean cool morning light."),
        slots=[
            dict(m="1", sem="cupboard", cn="挂墙杯架柜",
                 core="a wall-mounted coffee cup cabinet",
                 t=["a narrow bare three-tier wooden wall shelf, completely empty",
                    "the same shelf painted warm white, holding one neat row of twelve "
                    "plain white cups",
                    "the same cabinet with a muted sage-green back panel, slim brass hooks "
                    "and a tidy set of cups and saucers",
                    "the same cabinet as a tall floor-to-ceiling unit in warm white and "
                    "sage, a full set of premium cups and saucers and a warm hidden light "
                    "strip along the top"]),
            dict(m="2", sem="bottle_shelf", cn="吧台后酒水架",
                 core="a back-bar bottle and syrup shelf for a coffee bar",
                 t=["a single plain wall shelf with only two bottles standing on it",
                    "the same shelf as a two-tier pale ash wood shelf with six assorted "
                    "bottles and a few glasses",
                    "the same shelf with slim brass framing, glass dividers and a fuller "
                    "row of bottles and syrup bottles",
                    "the same shelf as a tall full-height back bar with a complete row of "
                    "bottles, syrups and glasses and a small spotlight above"]),
            dict(m="3", sem="stools", cn="高脚凳组",
                 core="a group of bar stools for a coffee bar",
                 t=["two simple bare folding stools standing side by side",
                    "the same spot with three plain pale wood bar stools in a neat row",
                    "the same three stools with soft sage-green leather seat pads and slim "
                    "brass legs",
                    "the same set as four bentwood bar chairs with backrests, padded sage "
                    "seats and brass foot caps"]),
            dict(m="4", sem="plant_stand", cn="绿植架",
                 core="an indoor plant stand for a coffee bar",
                 t=["one small green potted plant standing on the floor",
                    "the same spot with a three-tier pale wood stand holding three potted "
                    "green plants",
                    "the same stand as a white metal frame with five potted plants and one "
                    "small hanging basket",
                    "the same corner as a tall slim wrought-iron plant stand with eight "
                    "healthy plants and gentle trailing vines"]),
            dict(m="5", sem="menu_board", cn="立式菜单牌",
                 core="a freestanding cafe menu board",
                 t=["a rough wooden board with a blank sheet of paper pinned to it",
                    "the same board with a simple pale wood frame and a printed menu sheet",
                    "the same board as a proper freestanding blackboard with a wooden "
                    "frame and neat chalk handwriting",
                    "the same board as a polished slim brass-framed standing sign with a "
                    "glass cover and a small string of warm lights"]),
        ]),
    "z3": dict(
        prefix="z3shop",
        art=os.path.join(ART, "Zone3Shop"),
        label="Zone3 花园温室茶室",
        context=("A bright GARDEN GREENHOUSE TEA ROOM. Materials: whitewashed brick, "
                 "natural rattan and wicker, white wrought iron, blush-pink linen, warm "
                 "pale stone, bone china. Soft diffused sunlight."),
        slots=[
            dict(m="1", sem="display", cn="玻璃甜点陈列柜",
                 core="a glass dessert display case for a tea room",
                 t=["a small plain bare wooden cabinet holding only two small pastries",
                    "the same cabinet with simple glass panels and TWO tiers holding six "
                    "small cakes and tarts",
                    "the same cabinet with an arched glass top, THREE tiers and delicate "
                    "lace doilies under the pastries",
                    "the same cabinet as an ornate white metal-framed case with three "
                    "generously packed tiers of pastries, macarons and a warm interior "
                    "light"]),
            dict(m="2", sem="tea_sideboard", cn="茶具边柜",
                 core="a tea sideboard cabinet for a tea room",
                 t=["a plain bare wooden sideboard with a single simple teapot and cup on "
                    "top",
                    "the same sideboard painted warm white with two complete tea sets "
                    "arranged on it",
                    "the same sideboard with woven rattan drawer fronts, a small vase of "
                    "flowers and a neatly arranged tea set",
                    "the same sideboard with softly outlined panels, three fine bone-china "
                    "tea sets and a small polished tray"]),
            dict(m="3", sem="rattan_sofa", cn="藤编沙发座",
                 core="a rattan seating group for a tea room",
                 t=["a single worn old wicker chair standing alone",
                    "the same spot with a two-seat rattan bench and one blush-pink cushion",
                    "the same bench with two extra cushions and a small low tea table in "
                    "front of it",
                    "the same corner as a full rattan sofa set with three plump cushions, "
                    "a low tea table and a small flower stand beside it"]),
            dict(m="4", sem="flower_stand", cn="花架",
                 core="a flower stand for a greenhouse tea room",
                 t=["a single-tier wooden stand with only two small potted flowers",
                    "the same stand as a two-tier white metal stand with four potted "
                    "flowers",
                    "the same stand as a tiered ladder-style stand with six pots and "
                    "gentle trailing ivy",
                    "the same stand as an arched white wrought-iron flower stand with "
                    "eight pots and small hanging flower balls"]),
            dict(m="5", sem="tiered_stand", cn="三层点心塔（标志物）",
                 core=("an elegant three-tiered afternoon-tea dessert stand - the signature "
                       "centre-piece of the shop"),
                 t=["a small single-tier plate with only two slices of cake on it",
                    "the same piece becomes a two-tier stand with six small cakes and tarts",
                    "the same stand as a proper THREE-tier stand with every tier "
                    "generously filled with pastries and a glass cloche over the top",
                    "the same stand as an ornate three-tier stand, every tier heaped with "
                    "cakes, macarons and fruit tarts, finished with a small floral "
                    "arrangement on top"]),
        ]),
}


def fit_square(path, out, size=OG):
    """裁掉透明边 → 内容按最长边缩到 96% → 居中贴到透明方画布。"""
    im = Image.open(path).convert("RGBA")
    bbox = im.getbbox()
    if bbox:
        im = im.crop(bbox)
    pad = 0.96
    s = min(size[0] * pad / im.width, size[1] * pad / im.height)
    nw, nh = max(1, round(im.width * s)), max(1, round(im.height * s))
    cv = Image.new("RGBA", size, (0, 0, 0, 0))
    cv.paste(im.resize((nw, nh), Image.LANCZOS),
             ((size[0] - nw) // 2, (size[1] - nh) // 2))
    cv.save(out)
    return (nw, nh)


def run_skill(args, timeout=900):
    for att in range(4):
        r = subprocess.run([sys.executable, SKILL] + args, capture_output=True,
                           text=True, encoding="utf-8", errors="replace", timeout=timeout)
        if r.returncode == 0:
            return True, ""
        msg = ((r.stdout or "") + (r.stderr or ""))[-300:]
        if not any(x in msg.lower() for x in ("too many concurrent", "rate limit",
                                              "precharge", "timeout", "502", "503")):
            return False, msg
        time.sleep(min(2 ** att * 6, 45))
    return False, "重试耗尽"


def make_chain(zone_key, slot, tiers):
    """生成一个槽位的 tiers 档（链式）。返回 (成功档数, 日志行列表)。"""
    z = ZONES[zone_key]
    logs = []
    work = os.path.join(OUT, zone_key)
    os.makedirs(work, exist_ok=True)
    os.makedirs(z["art"], exist_ok=True)

    stem = "%s_building%s_%s" % (z["prefix"], slot["m"], slot["sem"])
    ok = 0
    prev = None
    for t in range(1, tiers + 1):
        dst = os.path.join(z["art"], "%s_%d.png" % (stem, t))

        # 断点续跑：已存在的档位直接用作后续参考，不重跑（省时省钱）
        if os.path.exists(dst) and os.path.getsize(dst) > 0:
            logs.append("  ⏭ %-42s t%d  已存在，跳过" % (stem, t))
            prev = dst
            ok += 1
            continue

        if t == 1:
            prompt = ("%s. %s %s Upgrade level 1 of 4: the most basic, humble and "
                      "worn-out version - simple material, plain finish, barely stocked."
                      % (slot["core"], STYLE, z["context"]))
            cmd = ["t2i", "--style", "bakery-painterly", "-p", prompt,
                   "--name", stem, "-s", "%dx%d" % OG, "--bg", "transparent",
                   "--soften", "-o", work]
        else:
            prompt = ("%s\nNew state: %s" % (TIER_NOTE % t, slot["t"][t - 1]))
            cmd = ["edit", "--style", "bakery-painterly", "-p", prompt,
                   "--ref", prev, "--ref-bg", "keep",
                   "--name", "%s_%d" % (stem, t), "-s", "%dx%d" % OG,
                   "--bg", "transparent", "--soften", "-o", work]

        good, err = run_skill(cmd)
        if not good:
            logs.append("  ❌ %-42s t%d  %s" % (stem, t, err[:100]))
            break

        src = os.path.join(work, "%s_%d.png" % (stem, t) if t > 1 else "%s.png" % stem)
        if not os.path.exists(src):
            cand = [f for f in os.listdir(work) if f.startswith(stem) and f.endswith(".png")]
            if not cand:
                logs.append("  ❌ %-42s t%d  产物缺失" % (stem, t))
                break
            src = os.path.join(work, sorted(cand)[-1])

        inner = fit_square(src, dst)
        logs.append("  ✅ %-42s t%d  内容 %dx%d → %s"
                    % (stem, t, inner[0], inner[1], os.path.basename(dst)))
        prev = dst
        ok += 1
    return ok, logs


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    ap = argparse.ArgumentParser()
    ap.add_argument("--apply", action="store_true")
    ap.add_argument("--only", help="z1 / z2:building5")
    ap.add_argument("--tiers", type=int, default=4)
    ap.add_argument("-j", "--jobs", type=int, default=3)
    a = ap.parse_args()

    zone_keys = sorted(ZONES)
    only_slot = None
    if a.only:
        parts = a.only.split(":")
        zone_keys = [parts[0]]
        if len(parts) > 1:
            only_slot = parts[1].replace("building", "").strip("_")

    jobs = []
    for zk in zone_keys:
        for slot in ZONES[zk]["slots"]:
            if only_slot and slot["m"] != only_slot:
                continue
            jobs.append((zk, slot))

    n_img = len(jobs) * a.tiers
    print("三场景店铺部件 · %d 个槽位 × %d 档 = %d 张\n" % (len(jobs), a.tiers, n_img))
    for zk, s in jobs:
        print("  %-4s building_%s  %-22s %s" % (zk, s["m"], s["cn"], s["core"][:44]))
    print("\n输出目录: Art/Scene/Zone{1,2,3}Shop/")

    if not a.apply:
        print("\n[dry-run] 加 --apply 执行。预估 $%.1f" % (n_img * 0.19))
        return

    os.makedirs(OUT, exist_ok=True)
    total = 0
    with ThreadPoolExecutor(max_workers=a.jobs) as ex:
        futs = {ex.submit(make_chain, zk, s, a.tiers): (zk, s) for zk, s in jobs}
        for f in futs:
            zk, s = futs[f]
            try:
                n, logs = f.result()
            except Exception as e:
                print("  💥 %s building_%s  %s" % (zk, s["m"], e))
                continue
            print("\n[%s building_%s %s]" % (zk, s["m"], s["cn"]))
            for ln in logs:
                print(ln)
            total += n

    print("\n完成：%d 张（目标 %d）" % (total, n_img))


if __name__ == "__main__":
    main()
