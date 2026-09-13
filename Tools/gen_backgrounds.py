# -*- coding: utf-8 -*-
"""三消关卡背景重绘（Scene/Backgrounds，4 张四季）。

用途核实：
  运行时实测 back_1 在关卡场景被渲染；下方须留出**干净纯色区**给棋盘摆放。

之前失败的原因：
  提示词只写了「四季场景」，模型把画面填满 → 下方留白消失 → 棋子放上去看不清。

本脚本的关键约束：
  画面**下方 1/4 必须是干净的平铺色块**（无装饰、无建筑、无遮挡），
  因为棋盘格要叠在这块区域上。

用法: python Tools/gen_backgrounds.py [--apply]
"""
import argparse
import os
import subprocess
import sys

from PIL import Image

SKILL = os.path.expanduser("~/.claude/skills/gpt-image-2/gpt_image_2.py")
HERE = os.path.dirname(os.path.abspath(__file__))
ART = os.path.join(HERE, "..", "Assets", "Project Files", "Game", "Art",
                   "Scene", "Backgrounds")
OUT = "D:/claudeWorkbase/_redraw/Scene/Backgrounds"

# 布局约束：上 3/4 是场景，下 1/4 是干净色块
LAYOUT = (
    "COMPOSITION: the scenery fills the FRAME from the top down to about 78 percent of "
    "the height, so the scene feels full and rich - sky, hills, buildings and trees all "
    "visible in the upper area. "
    "Only the BOTTOM 20 PERCENT (the lowest fifth) is a clean flat uniform ground colour "
    "band with no decoration, no buildings, no stones and no bushes - a calm empty strip "
    "where game tiles will be placed. "
    "Do not leave more than the bottom fifth empty; the scenery must come down close to it. "
)

SCENE = ("Cute casual mobile game level background, hand-painted illustration, "
         "soft matte finish, warm and cozy lighting, gentle depth. "
         "Slightly elevated view looking across the landscape. "
         "No text, no UI, no characters, no game pieces.")

ITEMS = {
    "back_1.png": ("SPRING DESSERT MEADOW",
                   "rolling bright green grass hills, pastel macaron-shaped mountains in "
                   "the far distance, a frosting-topped gingerbread cottage on the left, "
                   "round-headed green trees, cream sky with cotton clouds"),
    "back_2.png": ("WARM AUTUMN COUNTRYSIDE",
                   "soft rolling warm-yellow hills, a cream winding path, one slim green "
                   "tree and two yellow-leaved autumn trees, a tiny red-roofed cottage on "
                   "the left slope, warm goose-yellow sky with cotton clouds"),
    "back_3.png": ("WINTER SNOW VILLAGE",
                   "soft snowy rolling hills, a light-yellow-edged snow road, a cozy little "
                   "shop with a snow-capped roof and glowing windows on the left, conical "
                   "snow-covered trees, pale cream sky with cotton clouds"),
    "back_4.png": ("SUNSET SPRING COUNTRYSIDE",
                   "warm orange-to-pink gradient sunset sky, cotton clouds, soft hills with "
                   "reddish-brown accents, fresh green grass slopes, one round green tree "
                   "and two pink-blossom trees, a red-tiled cottage with glowing windows "
                   "on the left slope"),
}


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    ap = argparse.ArgumentParser()
    ap.add_argument("--apply", action="store_true")
    ap.add_argument("--only")
    a = ap.parse_args()

    names = sorted(ITEMS)
    if a.only:
        names = [x.strip() for x in a.only.split(",")]
    os.makedirs(OUT, exist_ok=True)

    print("四季背景重绘 · %d 张\n" % len(names))
    plan = []
    for n in names:
        title, desc = ITEMS[n]
        p = os.path.join(ART, n)
        size = Image.open(p).size if os.path.exists(p) else (2048, 2048)
        prompt = "%s. Scenery: %s. %s %s" % (title, desc, LAYOUT, SCENE)
        plan.append((n, size, prompt))
        print("  %-14s %-14s %s" % (n, "%dx%d" % size, title))

    if not a.apply:
        print("\n[dry-run] 加 --apply 执行。预估 $%.1f" % (len(plan) * 0.19))
        return

    ok = fail = 0
    import time as _t
    for n, size, prompt in plan:
        # 原图 2048x2048，API 支持
        cmd = [sys.executable, SKILL, "t2i", "--style", "bakery-painterly",
               "-p", prompt, "--name", os.path.splitext(n)[0],
               "-s", "%dx%d" % size, "--bg", "opaque", "-o", OUT]
        good = False
        for att in range(4):
            r = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8",
                               errors="replace", timeout=900)
            if r.returncode == 0:
                good = True
                break
            msg = ((r.stdout or "") + (r.stderr or ""))[-200:]
            if not any(k in msg.lower() for k in ("too many concurrent", "rate limit",
                                                  "precharge", "timeout", "502", "503")):
                print("  ❌ %-14s %s" % (n, msg[:110]))
                break
            _t.sleep(min(2 ** att * 5, 30))
        ok += good
        fail += (not good)
        if good:
            p2 = os.path.join(OUT, n)
            im = Image.open(p2).convert("RGB")
            if im.size != size:
                im = im.resize(size, Image.LANCZOS)
                im.save(p2)
            # 校验下方留白
            import numpy as np
            arr = np.asarray(Image.open(p2).convert("RGB")).astype(float)
            h, w, _ = arr.shape
            bottom = arr[int(h * 0.82):, :]
            std = bottom.reshape(-1, 3).std(axis=0).mean()
            print("  ✅ %-14s %dx%d  下方区标准差=%.1f %s"
                  % (n, size[0], size[1], std, "(留白OK)" if std < 12 else "(⚠留白不足)"))

    print("\n完成：成功 %d，失败 %d" % (ok, fail))


if __name__ == "__main__":
    main()
