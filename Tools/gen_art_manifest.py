# -*- coding: utf-8 -*-
"""生成换皮用美术资源索引 Art/_MANIFEST.md。

内容: 按归集后的分组列出全部使用中资源，含尺寸、原始路径（换皮时对照用）。
用法: python Tools/gen_art_manifest.py
"""
import os, json, struct, collections

ROOT = "Assets/Project Files/Game/Art"
MAP_FILE = "Tools/art_migration_map.json"
OUT = os.path.join(ROOT, "_MANIFEST.md")


def norm(p):
    return p.replace(os.sep, '/')


def png_size(path):
    try:
        with open(path, 'rb') as f:
            head = f.read(33)
        if head[:8] != b'\x89PNG\r\n\x1a\n':
            return None
        w, h = struct.unpack('>II', head[16:24])
        return w, h
    except OSError:
        return None


def jpg_size(path):
    try:
        with open(path, 'rb') as f:
            f.read(2)
            while True:
                b = f.read(1)
                while b and b != b'\xff':
                    b = f.read(1)
                while b == b'\xff':
                    b = f.read(1)
                if not b:
                    return None
                marker = b[0]
                if marker in (0xd8, 0xd9) or 0xd0 <= marker <= 0xd7:
                    continue
                ln = struct.unpack('>H', f.read(2))[0]
                if marker in (0xc0, 0xc1, 0xc2, 0xc3, 0xc5, 0xc6, 0xc7,
                              0xc9, 0xca, 0xcb, 0xcd, 0xce, 0xcf):
                    f.read(1)
                    h, w = struct.unpack('>HH', f.read(4))
                    return w, h
                f.seek(ln - 2, 1)
    except OSError:
        return None


def size(path):
    ext = os.path.splitext(path)[1].lower()
    if ext == '.png':
        return png_size(path)
    if ext in ('.jpg', '.jpeg'):
        return jpg_size(path)
    return None


def main():
    moved = json.load(open(MAP_FILE, encoding="utf-8"))
    src_of = {m["dst"]: m["src"] for m in moved}

    # 收集归集区所有美术文件
    files = []
    for dp, dn, fn in os.walk(ROOT):
        for f in fn:
            if f.endswith('.meta'):
                continue
            p = norm(os.path.join(dp, f))
            if os.path.splitext(f)[1].lower() in {'.png', '.jpg', '.jpeg', '.tga', '.psd'}:
                files.append(p)

    groups = collections.defaultdict(list)
    for p in files:
        rel = p[len(ROOT) + 1:]
        g = rel.rsplit('/', 1)[0] if '/' in rel else '.'
        groups[g].append(p)

    used_groups = {g: v for g, v in groups.items() if not g.startswith('_')}
    total = sum(len(v) for v in used_groups.values())

    lines = [
        "# CakeHome 美术资源索引（换皮用）",
        "",
        f"- 归集根目录: `{ROOT}/`",
        f"- 使用中资源: **{total}** 个，分布在 **{len(used_groups)}** 个分组",
        f"- `_Unused/` 未被任何 prefab/scene 引用；`_Pending/` 已生成但尚未接入",
        "- 全部资源通过 Unity GUID 被引用，**移动/改名目录不影响场景与预制体**",
        "- 换皮时直接覆盖同名文件即可，无需改动任何引用",
        "",
        "## 分组总览",
        "",
        "| 分组 | 数量 | 换皮说明 |",
        "|---|---|---|",
    ]

    NOTES = {
        "Gameplay/Pieces": "三消棋子（蛋糕/糖果），核心视觉",
        "Gameplay/Tiles": "棋盘格瓦片",
        "Gameplay/Tiles/Special": "特殊瓦片（冰/木箱/锁链）",
        "Gameplay/Chests": "宝箱开箱序列",
        "Gameplay/Characters": "角色立绘",
        "Gameplay/PowerUps": "道具图标",
        "Gameplay/Dock": "底部槽位底图",
        "Gameplay/Misc": "玩法杂项（经验条/锁/箭头）",
        "UI/Common": "通用面板、按钮、进度条",
        "UI/Icons": "全量功能图标",
        "UI/Store": "商店界面元素",
        "UI/Tutorial": "新手引导指针",
        "Dialog": "剧情弹窗与对话框",
        "Scene/Zone1": "Zone 1 关卡背景与布景",
        "Scene/Zone2": "Zone 2 关卡背景与布景",
        "Scene/Zone3": "Zone 3 关卡背景与布景",
        "Scene/Backgrounds": "关卡通用背景",
        "Map": "关卡地图",
        "Fx": "粒子与特效贴图",
        "Fonts/FredokaOne 50": "字体描边/发光贴图（小号）",
        "Fonts/FredokaOne 120": "字体描边/发光贴图（大号）",
    }
    for g in sorted(used_groups):
        lines.append(f"| `{g}/` | {len(used_groups[g])} | {NOTES.get(g, '')} |")

    lines += ["", "---", "", "## 资源明细", ""]

    for g in sorted(used_groups):
        lines.append(f"### {g}/  ({len(used_groups[g])})")
        lines.append("")
        lines.append("| 文件 | 尺寸 | 原始位置 |")
        lines.append("|---|---|---|")
        for p in sorted(used_groups[g]):
            name = os.path.basename(p)
            s = size(p)
            dim = f"{s[0]}×{s[1]}" if s else "—"
            old = src_of.get(p, "")
            lines.append(f"| `{name}` | {dim} | `{old}` |")
        lines.append("")

    # 附录: 未使用
    lines += ["---", "", "## 附录: 未接入资源", ""]
    for g in sorted(groups):
        if not g.startswith('_'):
            continue
        lines.append(f"### {g}/  ({len(groups[g])})")
        lines.append("")
        for p in sorted(groups[g]):
            s = size(p)
            dim = f"{s[0]}×{s[1]}" if s else "—"
            lines.append(f"- `{os.path.basename(p)}` ({dim})")
        lines.append("")

    open(OUT, "w", encoding="utf-8").write("\n".join(lines))
    print(f"已生成 {OUT}")
    print(f"使用中分组 {len(used_groups)} 个，资源 {total} 个")


if __name__ == "__main__":
    main()
