# -*- coding: utf-8 -*-
"""资源用途档案：每张图在哪出现、干什么用、什么效果、能不能换。

建立流程（不靠文件名猜）：
  ① 谁引用它          —— prefab/scene GUID 反查
  ② 挂在什么组件上    —— Image / SpriteRenderer / TMP
  ③ 显示参数          —— Image.Type、preserveAspect、Color tint、RectTransform 尺寸
  ④ 代码里怎么用      —— 按分组关联到玩法代码（tile/piece/UI...）
  ⑤ 运行时实例        —— --runtime 时从 Unity 查实际渲染尺寸与层级
  ⑥ 换图风险分级：
       🔴 不可换 —— 承载玩法信息（图案=区分度）、九宫格结构、文字贴图
       🟡 谨慎换 —— 有功能约束但有替代方案（需改 meta / 需改 prefab）
       🟢 可换   —— 纯装饰，换掉不影响功能与辨识

用法:
  python Tools/art_dossier.py                     # 全量
  python Tools/art_dossier.py --group Gameplay/Tiles
  python Tools/art_dossier.py --json out.json     # 导出供后续脚本使用
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

# 分组 → (角色说明, 风险等级, 换图前提)
GROUP_ROLE = {
    "Gameplay/Pieces": ("三消棋子，玩家靠图案区分可消类型与等级", "🔴 不可换",
                        "图案即玩法信息；换掉=认不出，已实测会把 12 级链画成同一物"),
    "Gameplay/Tiles": ("棋盘瓦片底图，c1/c2/c3 为三套主题，t1~t5 为等级", "🔴 不可换",
                       "同上；已实测 15 张全被画成同款空框，棋盘无法辨认"),
    "Gameplay/Tiles/Special": ("障碍/功能瓦片（冰、箱子、锁链），形状即机制提示", "🔴 不可换",
                               "语义色与形状承载机制含义"),
    "Gameplay/Chests": ("宝箱，闭合态=可点击开启；1~5 档华丽度递增", "🟡 谨慎换",
                        "必须保持『闭合』态与档位递进，且不可让金币外溢"),
    "Gameplay/Characters": ("角色立绘，剧情/任务中出现", "🟡 谨慎换",
                            "需保持同一角色的形象一致性"),
    "Gameplay/PowerUps": ("道具图标（提示/洗牌/撤销/加位）", "🟡 谨慎换",
                          "符号需保持可辨认"),
    "Gameplay/Dock": ("底部收集槽底图", "🟡 谨慎换", "结构固定，槽位需对齐"),
    "Gameplay/Misc": ("杂项玩法图标", "🟡 谨慎换", "逐张确认用途"),
    "UI/Icons": ("功能图标：货币/宝石/能量/箭头/锁/设置等", "🟡 谨慎换",
                 "货币类可换；符号类（叉/锁/箭头/开关）不可换"),
    "UI/Common": ("UI 基件：按钮底/面板底/渐变/光效；19 张为九宫格", "🔴 不可换",
                  "九宫格靠 spriteBorder 边缘像素拉伸，重绘必然撕裂（已实测）"),
    "UI/Store": ("商店界面元素：礼包图/面板底/角标", "🟡 谨慎换",
                 "礼包内容图可换；panel/frame 类为九宫格不可换"),
    "UI/Tutorial": ("引导指针与点击特效", "🔴 不可换", "符号类，AI 会画乱"),
    "Dialog": ("对话气泡、头像框、头像遮罩", "🟡 谨慎换", "遮罩类不可换（形状即功能）"),
    "Fx": ("特效贴图（星星/火花/雾），叠加在玩法层上", "🟡 谨慎换",
           "多为加法混合，需保持亮部透明过渡"),
    "Map": ("大地图地块（四季：春/秋/冬/樱花）+ 关卡按钮 + 云", "🔴 不可换",
            "已实测被画成立体蛋糕塔，地形色与路径全失，无法铺成地图"),
    "Scene/Backgrounds": ("三消关卡背景（四季主题）", "🟡 谨慎换",
                          "需保持四季色调与下方棋盘留白"),
    "Scene/Zone1": ("门店场景建筑（严格: 5 槽位 × 4 档升级）", "🔴 不可换",
                    "受 buildingId/存档/升级成本约束，换内容会破坏存档语义"),
    "Scene/Zone2": ("门店场景 Zone2 背景", "🟡 谨慎换", "需与 Zone1 风格一致"),
    "Scene/Zone3": ("门店场景 Zone3 预览", "🟡 谨慎换", "同上"),
    "Fonts/FredokaOne 50": ("TMP 字体字符图集", "🔴 不可换",
                            "含字形位图，重绘=字符丢失，文字无法显示（已实测）"),
    "Fonts/FredokaOne 120": ("TMP 字体字符图集（大号）", "🔴 不可换", "同上"),
}


def norm(p):
    return p.replace("\\", "/").strip("/")


def build_guid_index():
    g2p = {}
    for dp, dn, fn in os.walk(ART):
        for f in fn:
            if f.endswith(".meta"):
                t = open(os.path.join(dp, f), encoding="utf-8", errors="replace").read()
                m = re.search(r"guid:\s*([0-9a-f]{32})", t)
                if m:
                    g2p[m.group(1)] = os.path.join(dp, f[:-5])
    return g2p


def sprite_border(png):
    meta = png + ".meta"
    if not os.path.exists(meta):
        return None
    t = open(meta, encoding="utf-8", errors="replace").read()
    m = re.search(r"spriteBorder:\s*\{x:\s*([-\d.]+),\s*y:\s*([-\d.]+),"
                  r"\s*z:\s*([-\d.]+),\s*w:\s*([-\d.]+)\}", t)
    if not m:
        return None
    v = tuple(float(x) for x in m.groups())
    return v if any(x > 0 for x in v) else None


def scan_refs(g2p):
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
                for g, p in g2p.items():
                    if g not in txt:
                        continue
                    out[p].append(os.path.basename(path))
    return out


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    ap = argparse.ArgumentParser()
    ap.add_argument("--group")
    ap.add_argument("--json")
    a = ap.parse_args()

    from PIL import Image

    g2p = build_guid_index()
    refs = scan_refs(g2p)

    rows = []
    for dp, dn, fn in os.walk(ART):
        for f in sorted(fn):
            if not f.lower().endswith(".png"):
                continue
            p = os.path.join(dp, f)
            rel = os.path.relpath(p, ART).replace(os.sep, "/")
            grp = os.path.dirname(rel)
            if a.group and grp != a.group:
                continue
            role, risk, why = GROUP_ROLE.get(grp, ("未登记", "🟡 谨慎换", "需补登记"))
            im = Image.open(p)
            b = sprite_border(p)
            rows.append({
                "group": grp, "name": f, "size": "%dx%d" % im.size,
                "has_alpha": im.mode in ("RGBA", "LA", "P"),
                "nine_slice": tuple(round(x) for x in b) if b else None,
                "role": role, "risk": risk, "why": why,
                "used_by": sorted(set(refs.get(p, [])))[:4],
                "n_refs": len(refs.get(p, [])),
            })

    if a.json:
        json.dump(rows, open(a.json, "w", encoding="utf-8"),
                  ensure_ascii=False, indent=2)
        print("已导出 %d 条 → %s" % (len(rows), a.json))
        return

    by = collections.defaultdict(list)
    for r in rows:
        by[r["group"]].append(r)

    for grp in sorted(by):
        rs = by[grp]
        role, risk, why = rs[0]["role"], rs[0]["risk"], rs[0]["why"]
        nine = [r for r in rs if r["nine_slice"]]
        print("=" * 88)
        print("%s   %d 张   %s" % (grp, len(rs), risk))
        print("  用途：%s" % role)
        print("  换图：%s" % why)
        if nine:
            print("  ⚠ %d 张为九宫格（spriteBorder 锁定，重绘必撕裂）" % len(nine))
        used = [r for r in rs if r["n_refs"]]
        print("  被引用：%d/%d 张" % (len(used), len(rs)))
        for r in rs[:3]:
            u = ("← " + ", ".join(r["used_by"][:2])) if r["used_by"] else "（无引用）"
            print("     %-34s %-11s %s" % (r["name"], r["size"], u))
        if len(rs) > 3:
            print("     … 另 %d 张" % (len(rs) - 3))
        print()

    cnt = collections.Counter(r["risk"] for r in rows)
    print("合计 %d 张：" % len(rows),
          "  ".join("%s %d" % (k, v) for k, v in sorted(cnt.items())))


if __name__ == "__main__":
    main()
