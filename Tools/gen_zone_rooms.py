# -*- coding: utf-8 -*-
"""三场景室内店铺背景（Zone1 面包房 / Zone2 咖啡吧 / Zone3 温室茶室）。

用户要求（2026-09-13）：
  ① 背景**朴素简洁**——室内，不要豪华大理石厅
  ② **物件占比大**，升级品质差异是主体 → 背景必须低对比、低细节、留大片空墙
  ③ 三个场景三种不同风格
  ④ 每场景 5 个可升级部件
  → 完整设计见 docs/三场景室内店铺设计方案.md

关键布局约束（从 prefab 实测反推，必须遵守，否则部件会浮空/被裁）：
  画布 1080x1920，UI 坐标中心为原点、y 向上。5 个槽位的屏幕纵向范围：
    后墙件 B1/B2  中心 y=+225  → 距顶 395~1055 px
    中景件 B3/B4  中心 y=-310  → 距顶  940~1580 px
    前景件 B5     中心 y=-640  → 距顶 1320~1880 px
  即：**墙地交界线须落在距顶 55% 处**（1050/1920），下方 45% 是地板。
  墙地交界线以上是空墙（放后墙件），以下是进深地板（放中景/前景件）。

尺寸工艺（gpt-image-2 不支持 9:16）：
  生成 1024x1536(2:3) → 居中裁宽到 864x1536(9:16) → 放大到 1080x1920。
  裁掉的是左右各 8% 的墙边，对空房间无信息损失。

用法: python Tools/gen_zone_rooms.py [--apply] [--only z1,z2]
"""
import argparse
import os
import subprocess
import sys

from PIL import Image

SKILL = os.path.expanduser("~/.claude/skills/gpt-image-2/gpt_image_2.py")
HERE = os.path.dirname(os.path.abspath(__file__))
PROJ = os.path.normpath(os.path.join(HERE, ".."))
ART = os.path.join(PROJ, "Assets", "Project Files", "Game", "Art", "Scene")
OUT = "D:/claudeWorkbase/_zone_rooms"

# 目标 1080x1920；API 出 1024x1536 后裁到 9:16
GEN_SIZE = (1024, 1536)
FINAL = (1080, 1920)

# 构图约束：墙地交界在 55%，正中大片空墙，画面要“空”
LAYOUT = (
    "COMPOSITION (strict): a straight-on eye-level view of one empty room, "
    "simple one-point perspective looking directly at the back wall. "
    "The back wall occupies the upper part of the frame and the floor occupies the "
    "lower part; the wall-to-floor junction line sits at about 55 percent of the "
    "image height from the top. Above that line is plain empty wall with lots of "
    "clear undecorated surface; below it the bare floor recedes toward the viewer. "
    "The room is COMPLETELY EMPTY - no furniture, no shelves, no tables, no chairs, "
    "no products, no boxes, no plants on the floor. "
    "Keep the middle of the wall and the whole floor visually quiet, flat and low in "
    "contrast so that game objects placed on top will read clearly. "
    "No text, no letters, no signs, no logo, no people, no characters, no UI."
)

# 全局风格：沿用项目 bakery-painterly，但强调“朴素”
PLAIN = (
    "Plain and unpretentious interior, humble everyday shop, matte flat wall paint, "
    "simple honest materials with only light surface texture. Understated and clean, "
    "NOT luxurious, NOT ornate, no marble, no chandelier, no jewel tones, "
    "no heavy gold trim. Soft muted low-saturation warm palette, gentle even lighting, "
    "subtle ambient occlusion only where wall meets floor."
)

ROOMS = {
    "z1": dict(
        name="Zone1 温馨街角面包房",
        art=os.path.join(ART, "Zone1Shop"),
        file="z1shop_background.png",
        desc=(
            "A cozy little corner BAKERY interior. Cream-white painted plaster back wall "
            "with a simple light-oak wainscot panel on the lower third and a plain slim "
            "baseboard. Light oak plank wooden floor with soft visible grain. One modest "
            "square window with a plain white frame on the upper left of the back wall, "
            "letting in gentle warm afternoon daylight that falls softly across the wall. "
            "A single simple warm pendant lamp hanging from the ceiling slightly left of "
            "centre. Warm inviting small-business feeling, everything modest and handmade."
        ),
    ),
    "z2": dict(
        name="Zone2 清晨咖啡吧",
        art=os.path.join(HERE, "..", "Assets", "Project Files", "Game", "Art",
                         "Scene", "Zone2Shop"),
        file="z2shop_background.png",
        desc=(
            "A calm modern NORTHERN-EUROPEAN MINIMALIST COFFEE BAR interior. Warm-white "
            "back wall with a muted sage-green painted band across the lower half and a "
            "thin horizontal light-wood trim rail separating the two. Pale ash-grey wooden "
            "floor planks with a slim grey terrazzo skirting board. One tall narrow window "
            "on the upper RIGHT of the back wall with cool clean morning light streaming "
            "in. A row of two simple slim brass pendant lamps hanging above. "
            "Quiet Scandinavian simplicity, clean uncluttered surfaces, sage green and "
            "warm white only."
        ),
    ),
    "z3": dict(
        name="Zone3 花园温室茶室",
        art=os.path.join(HERE, "..", "Assets", "Project Files", "Game", "Art",
                         "Scene", "Zone3Shop"),
        file="z3shop_background.png",
        desc=(
            "A bright GARDEN GREENHOUSE TEA ROOM interior. Whitewashed painted brick back "
            "wall with a large arched window grid on the upper centre-left, thin dark-green "
            "window mullions, and soft blurred greenery visible outside the glass. "
            "Warm pale stone tile floor with a plain skirting. A delicate trailing "
            "wisteria vine hanging only along the very top edge of the wall near the "
            "ceiling, nothing hanging lower. Gentle diffused warm sunlight, airy and "
            "romantic but still simple. Blush pink, mint green and warm stone palette."
        ),
    ),
}


def fit(im, size=FINAL):
    """1024x1536(2:3) → 居中裁宽成 9:16 → 放大到 1080x1920。"""
    w, h = im.size
    tw = round(h * size[0] / size[1])          # 9:16 对应的宽
    if tw <= w:
        x = (w - tw) // 2
        im = im.crop((x, 0, x + tw, h))
    else:                                       # 反向：裁高
        th = round(w * size[1] / size[0])
        y = (h - th) // 2
        im = im.crop((0, y, w, y + th))
    return im.resize(size, Image.LANCZOS)


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    ap = argparse.ArgumentParser()
    ap.add_argument("--apply", action="store_true")
    ap.add_argument("--only")
    a = ap.parse_args()

    keys = sorted(ROOMS)
    if a.only:
        keys = [x.strip() for x in a.only.split(",")]
    os.makedirs(OUT, exist_ok=True)

    print("三场景室内店铺背景 · %d 张\n" % len(keys))
    plan = []
    for k in keys:
        r = ROOMS[k]
        prompt = "%s %s %s" % (r["desc"], LAYOUT, PLAIN)
        plan.append((k, r, prompt))
        print("  %-4s %-22s → %s  %dx%d" % (k, r["name"], r["file"], *FINAL))

    if not a.apply:
        print("\n[dry-run] 加 --apply 执行。预估 $%.2f" % (len(plan) * 0.28))
        return

    ok = fail = 0
    import time as _t
    for k, r, prompt in plan:
        cmd = [sys.executable, SKILL, "t2i", "--style", "bakery-painterly",
               "-p", prompt, "--name", os.path.splitext(r["file"])[0],
               "-s", "%dx%d" % GEN_SIZE, "--bg", "opaque", "--soften",
               "-o", OUT]
        good = False
        for att in range(4):
            res = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8",
                                 errors="replace", timeout=900)
            if res.returncode == 0:
                good = True
                break
            msg = ((res.stdout or "") + (res.stderr or ""))[-200:]
            if not any(x in msg.lower() for x in ("too many concurrent", "rate limit",
                                                  "precharge", "timeout", "502", "503")):
                print("  ❌ %-4s %s" % (k, msg[:130]))
                break
            _t.sleep(min(2 ** att * 5, 30))
        ok += good
        fail += (not good)
        if good:
            src = os.path.join(OUT, r["file"])
            im = fit(Image.open(src).convert("RGB"))
            # 落位到各自 Zone 目录
            os.makedirs(r["art"], exist_ok=True)
            dst = os.path.join(r["art"], r["file"])
            im.save(dst)
            print("  ✅ %-4s %s → %s  %dx%d"
                  % (k, os.path.basename(src), dst, *im.size))

    print("\n完成：成功 %d，失败 %d" % (ok, fail))


if __name__ == "__main__":
    main()
