# -*- coding: utf-8 -*-
"""为缺 .meta 的 PNG 生成 .meta（复用项目内同类型贴图的模板）。

背景：新生成的 PNG 在 Unity 还没导入前没有 .meta，prefab 无法引用（拿不到 guid）。
      本脚本用项目里已有的 Scene 贴图 .meta 作模板，只替换 guid，保证导入参数一致。

顺带把 maxTextureSize 按 PNG 实际尺寸设为最近的 2 的幂（避免被 Unity 缩小掉细节）。

用法:
  python Tools/ensure_png_meta.py                        # dry-run
  python Tools/ensure_png_meta.py --apply
  python Tools/ensure_png_meta.py --apply --dir "Assets/Project Files/Game/Art/Scene"
"""
import argparse
import hashlib
import os
import re
import sys

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
PROJ = os.path.normpath(os.path.join(HERE, ".."))

# 模板必须满足：textureType: 8 (Sprite) + spriteMode: 1 (Single)
#   ⚠ 踩过的坑：误用了 textureType: 0 (Default) 的贴图当模板 →
#      Unity 不生成 sprite 子资源 → prefab 里 m_Sprite 引用全部变 NULL（画面空白）。
#   用项目内已验证可用的场景部件图当模板。
TEMPLATE = os.path.join(PROJ, "Assets", "Project Files", "Game", "Art", "Scene",
                        "Zone1", "z1_house_1.png.meta")


def new_guid(path):
    """由路径稳定派生 guid（同路径重复执行结果一致）。"""
    return hashlib.md5(("cakehome:" + path.replace("\\", "/")).encode()).hexdigest()


def next_pow2(n):
    p = 64
    while p < n and p < 8192:
        p *= 2
    return p


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    ap = argparse.ArgumentParser()
    ap.add_argument("--apply", action="store_true")
    ap.add_argument("--dir", default=os.path.join("Assets", "Project Files", "Game", "Art"))
    ap.add_argument("--template", help="指定 .meta 模板")
    a = ap.parse_args()

    root = a.dir if os.path.isabs(a.dir) else os.path.join(PROJ, a.dir)
    tpl_path = a.template or TEMPLATE
    if not os.path.exists(tpl_path):
        sys.exit("模板不存在: %s" % tpl_path)
    tpl = open(tpl_path, encoding="utf-8", errors="replace").read()

    # 模板健全性自检：必须是 Sprite 类型，否则生成的 .meta 会让引用变 NULL
    if "textureType: 8" not in tpl or "spriteMode: 1" not in tpl:
        sys.exit("模板不是 Sprite 贴图（需 textureType: 8 / spriteMode: 1）: %s" % tpl_path)

    todo = []
    for dp, dn, fn in os.walk(root):
        for f in fn:
            if not f.endswith(".png"):
                continue
            p = os.path.join(dp, f)
            if os.path.exists(p + ".meta"):
                continue
            try:
                w, h = Image.open(p).size
            except Exception:
                continue
            todo.append((p, (w, h)))

    print("缺 .meta 的 PNG：%d 张（模板 %s）\n" % (len(todo), os.path.basename(tpl_path)))
    for p, sz in todo[:40]:
        print("  %-58s %dx%d" % (os.path.relpath(p, PROJ), sz[0], sz[1]))
    if len(todo) > 40:
        print("  … 另 %d 张" % (len(todo) - 40))

    if not a.apply:
        print("\n[dry-run] 加 --apply 生成")
        return

    n = 0
    for p, (w, h) in todo:
        rel = os.path.relpath(p, PROJ).replace(os.sep, "/")
        t = re.sub(r"guid: [a-f0-9]{32}", "guid: %s" % new_guid(rel), tpl, count=1)
        need = next_pow2(max(w, h))
        t = re.sub(r"(maxTextureSize: )\d+", r"\g<1>%d" % need, t)
        # 部件贴图用 alphaIsTransparency=1 更正确（模板本身是 1 就不必改）
        if "alphaIsTransparency: 0" in t:
            t = t.replace("alphaIsTransparency: 0", "alphaIsTransparency: 1")
        open(p + ".meta", "w", encoding="utf-8", newline="\n").write(t)
        n += 1
    print("\n已生成 %d 个 .meta（guid 由路径派生，maxTextureSize 按实际尺寸）" % n)


if __name__ == "__main__":
    main()
