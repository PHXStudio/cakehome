# -*- coding: utf-8 -*-
"""按 "风险从低到高" 排定 Art/ 资源的分批重绘计划。

风险评估维度：
  尺寸      —— 小图放大后细节丢失不可逆；大图安全
  透明      —— 带透明区的图标/棋子，AI 重绘后透明易丢，需 RMBG 兜底
  形状敏感  —— 符号类（叉/勾/箭头/开关）AI 会画乱，风险最高
  数量      —— 影响单批成本与耗时

用法: python Tools/art_batch_plan.py [--json]
"""
import json
import os
import sys
import collections

from PIL import Image

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                    "..", "Assets", "Project Files", "Game", "Art")

# 符号类关键词：AI 重绘会把内部符号画丢/画乱，最高风险
SYMBOL_HINTS = ("cross", "close", "check", "arrow", "plus", "minus", "play",
                "pause", "skip", "toggle", "switch", "lock", "question",
                "settings", "gear", "undo", "indicator", "star_empty")

# 分组 → 批次归属（按风险从低到高）
BATCHES = [
    {
        "id": 1, "name": "背景板",
        "risk": "低", "why": "大尺寸、不透明，与参考图的场景风格天然契合，无透明冲突",
        "includes": ["Scene/Backgrounds", "Scene/Zone2", "Scene/Zone3",
                     "_Pending/Zone1Shop/full"],
    },
    {
        "id": 2, "name": "场景布景",
        "risk": "低", "why": "大尺寸物件图，透明区形状简单，RMBG 易兜底",
        "includes": ["Scene/Zone1"],
    },
    {
        "id": 3, "name": "玩法主体",
        "risk": "中", "why": "棋子/瓦片是玩法核心，数量大但造型简单重复，失败可整批重跑",
        "includes": ["Gameplay/Pieces", "Gameplay/Tiles", "Gameplay/Tiles/Special"],
    },
    {
        "id": 4, "name": "玩法道具",
        "risk": "中", "why": "宝箱/角色/道具，造型有辨识度要求，需逐张确认",
        "includes": ["Gameplay/Chests", "Gameplay/Characters",
                     "Gameplay/PowerUps", "Gameplay/Dock", "Gameplay/Misc"],
    },
    {
        "id": 5, "name": "UI 图标",
        "risk": "中高", "why": "含符号类图标（叉/锁/设置），AI 易画乱，需 t2i 重建或保留几何",
        "includes": ["UI/Icons", "UI/Tutorial"],
    },
    {
        "id": 6, "name": "UI 通用",
        "risk": "高", "why": "面板九宫格/按钮底依赖边缘约束，重绘会破坏拉伸规则",
        "includes": ["UI/Common", "UI/Store"],
    },
    {
        "id": 7, "name": "地图",
        "risk": "中", "why": "超大尺寸(2200x2400)，成本高、需确认 API 是否支持",
        "includes": ["Map"],
    },
    {
        "id": 8, "name": "剧情与特效",
        "risk": "中高", "why": "对话头像有角色一致性要求；特效多为叠加层，重绘可能失效",
        "includes": ["Dialog", "Fx"],
    },
    {
        "id": 9, "name": "字体贴图",
        "risk": "高", "why": "美术字贴图与字形绑定，重绘会破坏字面",
        "includes": ["Fonts"],
    },
    {
        "id": 10, "name": "待接入 / 孤儿",
        "risk": "低", "why": "_Pending 尚未接入；_Unused 无引用。是否重绘取决于是否启用",
        "includes": ["_Pending", "_Unused"],
    },
]


def scan():
    groups = collections.defaultdict(list)
    for dp, dn, fn in os.walk(ROOT):
        for f in fn:
            if f.lower().endswith(".png"):
                rel = os.path.relpath(dp, ROOT).replace(os.sep, "/")
                groups[rel].append(os.path.join(dp, f))
    return groups


def profile(files):
    sizes = collections.Counter()
    alpha = 0
    for p in files:
        try:
            im = Image.open(p)
            sizes[im.size] += 1
            if im.mode in ("RGBA", "LA", "P"):
                if im.convert("RGBA").getchannel("A").getextrema()[0] < 250:
                    alpha += 1
        except Exception:
            pass
    w = sum(s[0] * c for s, c in sizes.items()) / max(1, len(files))
    h = sum(s[1] * c for s, c in sizes.items()) / max(1, len(files))
    return w, h, alpha


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    groups = scan()
    assigned = set()
    out = []

    for b in BATCHES:
        files, detail = [], []
        for inc in b["includes"]:
            for g, fs in sorted(groups.items()):
                if g == inc or g.startswith(inc + "/"):
                    fs = [f for f in fs if f not in assigned]   # 去重
                    if not fs:
                        continue
                    files += fs
                    detail.append((g, len(fs)))
        assigned |= set(files)
        w, h, alpha = profile(files)
        out.append({"id": b["id"], "name": b["name"], "risk": b["risk"],
                    "why": b["why"], "count": len(files),
                    "avg_size": "%dx%d" % (round(w), round(h)),
                    "alpha": alpha, "groups": detail, "files": files})

    left = [f for fs in groups.values() for f in fs if f not in assigned]
    if left:
        out.append({"id": 99, "name": "未归入", "risk": "-", "why": "",
                    "count": len(left), "avg_size": "-", "alpha": 0,
                    "groups": [], "files": left})

    if "--json" in sys.argv:
        print(json.dumps([{k: v for k, v in o.items() if k != "files"} for o in out],
                         ensure_ascii=False, indent=2))
        return

    total = 0
    print("%-4s%-14s%-6s%7s%13s%10s  %s" % ("批", "名称", "风险", "张数", "平均尺寸", "带透明", "说明"))
    print("-" * 118)
    for o in out:
        print("%-4s%-14s%-6s%7d%13s%10s  %s" % (
            o["id"], o["name"], o["risk"], o["count"], o["avg_size"],
            "%d/%d" % (o["alpha"], o["count"]), o["why"][:46]))
        total += o["count"]
    print("-" * 118)
    print("合计 %d 张  |  预估成本 $%.1f（high 质量 $0.19/张）" % (total, total * 0.19))
    print("预估耗时：4 并发约 %.1f 小时" % (total * 145 / 4 / 3600))


if __name__ == "__main__":
    main()
