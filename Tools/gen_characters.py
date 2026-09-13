# -*- coding: utf-8 -*-
"""人物立绘重绘——保持欧美拟真写实风，重新创作以规避素材版权。

背景（用户要求）：
  现有 6 张立绘疑似第三方素材包，有**版权风险**，需要重新创作。
  但必须**保持欧美拟真人物风格**（不能变成日系 Q 版/萌系）。

原有设定（从 `Data/Characters/*.asset` 读出，必须保留）：
  C1 Maya   主角，绿色主题（色相 100°）
  C2 Daniel 蓝主题（209°）
  C3 Iris   橙主题（34°）
  C4 Theo   红主题（0°）
  C5 Sofia  粉紫主题（305°）
  C6 Arthur 紫主题（275°）

**失败教训（本脚本要避免的）**：
  第一次重绘时提示词写了 "Q-version chibi proportion" →
  产出全部变成日系萌系小女孩，与原欧美写实风完全冲突，且 6 个角色失去区分度。

本脚本的提示词要点：
  ① 明确指定 **semi-realistic Western illustration**（半写实欧美插画）
  ② 明确禁用 anime / chibi / manga / kawaii
  ③ 每个角色给**独立的外貌描述**（年龄/肤色/发色/服装）保持区分度
  ④ 构图与原图一致：半身像、正面、双手插兜或自然姿态、透明背景

用法: python Tools/gen_characters.py [--apply] [--only C1,C2]
"""
import argparse
import os
import subprocess
import sys

from PIL import Image

SKILL = os.path.expanduser("~/.claude/skills/gpt-image-2/gpt_image_2.py")
HERE = os.path.dirname(os.path.abspath(__file__))
ART = os.path.join(HERE, "..", "Assets", "Project Files", "Game", "Art",
                   "Gameplay", "Characters")
OUT = "D:/claudeWorkbase/_redraw/Gameplay/Characters"

# 风格约束：欧美半写实，明确排除日系
STYLE = (
    "STYLE: modern Western semi-realistic character illustration for a casual "
    "mobile game. Painted in a polished cartoon-realistic style with believable "
    "anatomy and proportions - roughly 6 to 7 heads tall, adult proportions, "
    "natural face structure with a defined nose and jawline, realistic eyes (not "
    "oversized), natural skin tones and soft cel shading with gentle rim light. "
    "ABSOLUTELY NOT anime, NOT manga, NOT chibi, NOT Q-version, NOT kawaii, "
    "NOT big sparkly eyes, NOT tiny nose-dot faces, NOT super-deformed bodies. "
)

POSE = (
    "Composition: upper-body portrait from the waist up, facing the viewer three-quarters "
    "on, relaxed friendly posture with hands in pockets, warm confident smile. "
    "Isolated cutout on a fully transparent background, no scenery, no shadow on the "
    "background, no text, no border, no frame. "
    "Consistent character design across the set so the six characters look like they "
    "belong to the same game."
)

# 每个角色独立描述（保留原设定：性别/年龄/肤色/发色/服装主色）
CHARS = {
    "C1": ("game_character_1.png", "Maya",
           "a young woman in her mid twenties with light olive skin and long wavy "
           "chestnut-brown hair tied in a loose top bun. She wears an open fresh "
           "sage-green utility jacket over a cream ribbed tank top and light beige "
           "trousers. Small gold hoop earrings. Bright, capable, friendly."),
    "C2": ("game_character_2.png", "Daniel",
           "a young man in his mid twenties with warm tan skin and short neat black "
           "hair. He wears an open dark navy-blue overshirt over a plain light grey "
           "t-shirt and dark blue jeans. Calm, easy-going and approachable."),
    "C3": ("game_character_3.png", "Iris",
           "an elegant older woman in her sixties with fair skin and a short wavy "
           "silver-white bob. She wears a deep red open cardigan over a soft cream "
           "blouse and beige trousers, with a simple gold pendant necklace. "
           "Warm and grandmotherly."),
    "C4": ("game_character_4.png", "Theo",
           "a young man in his mid twenties with deep brown skin and short cropped "
           "black hair with a neat fade. He wears an open forest-green utility jacket "
           "over a cream henley shirt and blue jeans. Cheerful and energetic."),
    "C5": ("game_character_5.png", "Sofia",
           "a young woman in her mid twenties with fair skin and shoulder-length "
           "golden-blonde wavy hair. She wears a light sage-green open shirt over a "
           "cream camisole and light cream trousers, with a delicate gold necklace. "
           "Warm, sunny and welcoming."),
    "C6": ("game_character_6.png", "Arthur",
           "a young man in his mid twenties with fair skin and short wavy dark brown "
           "hair. He wears an open light beige linen overshirt over a plain white "
           "t-shirt and blue jeans, with a thin silver chain necklace. "
           "Confident and friendly."),
}


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    ap = argparse.ArgumentParser()
    ap.add_argument("--apply", action="store_true")
    ap.add_argument("--only", help="如 C1,C2")
    a = ap.parse_args()

    keys = sorted(CHARS)
    if a.only:
        keys = [x.strip() for x in a.only.split(",")]
    os.makedirs(OUT, exist_ok=True)

    print("人物立绘重绘（欧美半写实，重新创作） · %d 张\n" % len(keys))
    plan = []
    for k in keys:
        fn, name, desc = CHARS[k]
        src = os.path.join(ART, fn)
        size = Image.open(src).size if os.path.exists(src) else (1024, 1024)
        prompt = "A character portrait of %s, %s. %s %s" % (name, desc, STYLE, POSE)
        plan.append((fn, size, name, prompt))
        print("  %-24s %-8s %-12s %s" % (fn, k, name, "%dx%d" % size))

    if not a.apply:
        print("\n[dry-run] 加 --apply 执行。预估 $%.1f" % (len(plan) * 0.19))
        return

    ok = fail = 0
    import time as _t
    for fn, size, name, prompt in plan:
        cmd = [sys.executable, SKILL, "t2i", "--style", "bakery-painterly",
               "-p", prompt, "--name", os.path.splitext(fn)[0],
               "-s", "%dx%d" % size, "--bg", "transparent", "-o", OUT]
        good = False
        for att in range(4):
            r = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8",
                               errors="replace", timeout=900)
            if r.returncode == 0:
                good = True
                break
            msg = ((r.stdout or "") + (r.stderr or ""))[-200:]
            if not any(x in msg.lower() for x in ("too many concurrent", "rate limit",
                                                  "precharge", "timeout", "502", "503")):
                print("  ❌ %-24s %s" % (fn, msg[:110]))
                break
            _t.sleep(min(2 ** att * 5, 30))
        ok += good
        fail += (not good)
        if good:
            p2 = os.path.join(OUT, fn)
            im = Image.open(p2).convert("RGBA")
            if im.size != size:
                im = im.resize(size, Image.LANCZOS)
                im.save(p2)
            print("  ✅ %-24s %s %dx%d" % (fn, name, size[0], size[1]))

    print("\n完成：成功 %d，失败 %d" % (ok, fail))


if __name__ == "__main__":
    main()
