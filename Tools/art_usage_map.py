# -*- coding: utf-8 -*-
"""建立「资源 → 游戏内用途」档案：谁引用它、挂在什么组件上、显示成什么。

为什么要做这个：
  之前按文件名和「看起来像什么」批量重绘，导致 Map 被画成蛋糕塔、
  Tiles 被画成空框、字体贴图被画成相框 —— 因为没有先搞清楚每张图
  **在游戏里出现在哪、干什么用、什么效果**。

本脚本从三处反查，不靠猜：
  ① prefab / scene 的 GUID 引用 —— 谁在用它
  ② 引用处的组件类型（Image / SpriteRenderer / ...）—— 怎么用的
  ③ 组件的关键属性（Image.Type=Sliced? preserveAspect? 尺寸?）—— 什么效果
  ④ （可选）运行时从 Unity 反查实际显示尺寸与位置

用法:
  python Tools/art_usage_map.py                 # 静态反查（快）
  python Tools/art_usage_map.py --runtime       # 额外查 Unity 运行时实例
  python Tools/art_usage_map.py --group Map     # 只看某组
"""
import argparse
import collections
import json
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
PROJ = os.path.normpath(os.path.join(HERE, ".."))
ART = os.path.join(PROJ, "Assets", "Project Files", "Game", "Art")
SCAN_DIRS = [os.path.join(PROJ, "Assets", "Project Files", "Game", "Prefabs"),
             os.path.join(PROJ, "Assets", "Project Files", "Game", "Scenes")]


def build_guid_index():
    """guid → art 资源路径"""
    g2p = {}
    for dp, dn, fn in os.walk(ART):
        for f in fn:
            if f.endswith(".meta"):
                t = open(os.path.join(dp, f), encoding="utf-8", errors="replace").read()
                m = re.search(r"guid:\s*([0-9a-f]{32})", t)
                if m:
                    g2p[m.group(1)] = os.path.join(dp, f[:-5])
    return g2p


def scan_refs(g2p):
    """扫描 prefab/scene，找出每个 art guid 被谁引用、什么组件、什么属性。"""
    out = collections.defaultdict(list)
    for base in SCAN_DIRS:
        for dp, dn, fn in os.walk(base):
            for f in fn:
                if not (f.endswith(".prefab") or f.endswith(".unity")):
                    continue
                path = os.path.join(dp, f)
                try:
                    txt = open(path, encoding="utf-8", errors="replace").read()
                except Exception:
                    continue
                if not any(g in txt for g in g2p):
                    continue
                # 按文档块切分，逐块找 guid + 组件类型 + 关键属性
                for block in re.split(r"\n--- ", txt):
                    guids = set(re.findall(r"guid:\s*([0-9a-f]{32})", block))
                    hit = [g for g in guids if g in g2p]
                    if not hit:
                        continue
                    comp = re.search(r"^!u!(\d+)", block)
                    ctype = {"114": "MonoBehaviour", "212": "SpriteRenderer",
                             "222": "CanvasRenderer", "223": "MeshRenderer",
                             "224": "Renderer"}.get(
                        comp.group(1) if comp else "", "?")
                    if "m_Sprite:" in block:
                        ctype = "Image/SpriteRenderer"
                    props = {}
                    for k in ("m_Type", "m_PreserveAspect", "m_FillMethod",
                              "m_FillAmount", "m_Color", "m_Sprite"):
                        m = re.search(k + r":\s*([^\n]+)", block)
                        if m:
                            props[k] = m.group(1).strip()[:60]
                    name = re.search(r"m_Name:\s*([^\n]+)", block)
                    go = name.group(1).strip() if name else ""
                    for g in hit:
                        out[g2p[g]].append({
                            "file": os.path.relpath(path, PROJ).replace(os.sep, "/"),
                            "go": go, "ctype": ctype, "props": props,
                        })
    return out


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    ap = argparse.ArgumentParser()
    ap.add_argument("--group", help="只看某分组")
    ap.add_argument("--json", action="store_true")
    a = ap.parse_args()

    g2p = build_guid_index()
    refs = scan_refs(g2p)

    rows = []
    for p, uses in refs.items():
        rel = os.path.relpath(p, ART).replace(os.sep, "/")
        grp = os.path.dirname(rel)
        if a.group and grp != a.group:
            continue
        rows.append({"group": grp, "name": os.path.basename(rel),
                     "path": rel, "uses": uses})

    if a.json:
        print(json.dumps(rows, ensure_ascii=False, indent=2))
        return

    by = collections.defaultdict(list)
    for r in rows:
        by[r["group"]].append(r)

    for grp in sorted(by):
        print("=" * 86)
        print("%s   （%d 张有引用）" % (grp, len(by[grp])))
        print("=" * 86)
        for r in by[grp]:
            us = r["uses"][:2]
            for u in us:
                prop = u["props"].get("m_Type", "")
                pa = "preserveAspect" if "1" in u["props"].get("m_PreserveAspect", "") else ""
                print("  %-34s → %s" % (r["name"], u["file"].split("/")[-1]))
                print("       组件=%s  对象=%s  %s %s"
                      % (u["ctype"], u["go"] or "?", prop, pa))
            if len(r["uses"]) > 2:
                print("       … 另被 %d 处引用" % (len(r["uses"]) - 2))
        print()

    n_orphan = sum(1 for p in g2p.values()
                   if os.path.relpath(p, ART).replace(os.sep, "/").startswith(("UI", "Gameplay", "Map", "Dialog", "Fx", "Scene", "Fonts"))
                   and p not in refs)
    print("有引用 %d 张 | 未被 prefab/scene 引用 %d 张" % (len(rows), n_orphan))


if __name__ == "__main__":
    main()
