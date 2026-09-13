# -*- coding: utf-8 -*-
"""CakeHome 美术资源归集脚本。

把项目自有美术资源统一归集到 Assets/Project Files/Game/Art/ 下，按换皮视角分组。

用法:
    python Tools/migrate_art.py            # dry-run，只打印计划
    python Tools/migrate_art.py --apply    # 实际执行
    python Tools/migrate_art.py --rollback # 按映射表回滚

设计要点:
- 移动文件时连同 .meta 一起移动 → Unity GUID 引用（prefab/scene/asset）完全不受影响
- 唯一需要同步修改的是 .cs 里硬编码的 AssetDatabase.LoadAssetAtPath 路径
- 重名冲突自动加来源后缀
- 生成 Tools/art_migration_map.json 作为回滚依据
"""
import os, re, json, shutil, sys, collections

SRC_REPORT = "Tools/art_refs.json"
MAP_FILE = "Tools/art_migration_map.json"
DST_ROOT = "Assets/Project Files/Game/Art"

# 明确的目录级映射规则
DIR_RULES = {
    "Assets/Project Files/Game/Images/Backgrounds":        "Scene/Backgrounds",
    "Assets/Project Files/Game/Images/Base/Currency":      "UI/Common",
    "Assets/Project Files/Game/Images/Base/General UI":    "UI/Common",
    "Assets/Project Files/Game/Images/Base/Misc":          "UI/Common",
    "Assets/Project Files/Game/Images/Base/Settings":      "UI/Icons",
    "Assets/Project Files/Game/Images/Base/Store":         "UI/Store",
    "Assets/Project Files/Game/Images/Base/Tutorial":      "UI/Tutorial",
    "Assets/Project Files/Game/Images/Dialogs":            "Dialog",
    "Assets/Project Files/Game/Images/Dock":               "Gameplay/Dock",
    "Assets/Project Files/Game/Images/Gameplay":           "Gameplay/Pieces",
    "Assets/Project Files/Game/Images/Icon":               "UI/Icons",
    "Assets/Project Files/Game/Images/Map":                "Map",
    "Assets/Project Files/Game/Images/Particles":          "Fx",
    "Assets/Project Files/Game/Images/Power Ups":          "Gameplay/PowerUps",
    "Assets/Project Files/Game/Images/Tiles":              "Gameplay/Tiles",
    "Assets/Project Files/Game/Images/Tiles/Special":      "Gameplay/Tiles/Special",
    "Assets/Project Files/Game/Images/UI":                 "UI/Common",
    "Assets/Project Files/Game/Images/Zone 1":             "Scene/Zone1",
    "Assets/Project Files/Game/Images/Zone 2":             "Scene/Zone2",
    "Assets/Project Files/Game/Images/Zone 3":             "Scene/Zone3",
    "Assets/Project Files/Game/Fonts/FredokaOne/Fredoka One 120": "Fonts/FredokaOne 120",
    "Assets/Project Files/Game/Fonts/FredokaOne/FredokaOne 50":   "Fonts/FredokaOne 50",
    "Assets/HotUpdate/Scripts/Tutorial/Images":            "UI/Tutorial",
}

IMAGES_ROOT = "Assets/Project Files/Game/Images"


def norm(p):
    return p.replace(os.sep, '/')


def classify_used(src):
    """使用中资源 → Art/ 下的相对目标目录"""
    d = os.path.dirname(src)
    name = os.path.basename(src)

    if d == IMAGES_ROOT:
        if name.startswith("dialog_"):
            return "Dialog"
        if name.startswith("game_character"):
            return "Gameplay/Characters"
        if name.startswith("game_chest"):
            return "Gameplay/Chests"
        if name.startswith("game_"):
            return "Gameplay/Misc"
        if name.startswith("ui_icon_"):
            return "UI/Icons"
        if name.startswith("ui_"):
            return "UI/Common"
        return "Gameplay/Misc"

    return DIR_RULES.get(d)


def classify_orphan(src):
    """孤儿资源 → _Unused/ 或 _Pending/（Zone1 烘焙店素材是待接入的新资源）"""
    if "Zone 1 Shop/zone1_generated" in norm(src):
        # .../zone1_generated/objects/counter/xxx_tier1.png → objects/counter
        rel = norm(src).split("zone1_generated/", 1)[1]
        return f"_Pending/Zone1Shop/{os.path.dirname(rel)}"
    if "/Textures/CandyTiles/" in norm(src):
        return "_Unused/CandyTiles"
    if "/Game/Fonts/" in norm(src):
        sub = norm(src).split("/FredokaOne/", 1)[-1].rsplit("/", 1)[0]
        return f"_Unused/Fonts/{sub}"
    # 其余按原始相对路径保留结构
    rel = norm(src).split("Game/Images/", 1)[-1]
    sub = os.path.dirname(rel)
    return f"_Unused/{sub}" if sub else "_Unused"


def build_plan():
    rep = json.load(open(SRC_REPORT, encoding="utf-8"))
    plan = []          # (src, dst)
    taken = {}         # dst -> src

    for src in sorted(rep["used"]):
        sub = classify_used(src)
        if sub is None:
            print(f"  [警告] 无匹配规则，归入 UI/Common: {src}")
            sub = "UI/Common"
        plan.append((src, f"{DST_ROOT}/{sub}/{os.path.basename(src)}"))

    for src in sorted(rep["orphans"]):
        sub = classify_orphan(src)
        plan.append((src, f"{DST_ROOT}/{sub}/{os.path.basename(src)}"))

    # 重名冲突处理
    final = []
    for src, dst in plan:
        if dst in taken:
            base, ext = os.path.splitext(dst)
            tag = os.path.basename(os.path.dirname(src)).replace(" ", "")
            new = f"{base}__{tag}{ext}"
            print(f"  [重名] {dst}\n         ← {src}\n         → {new}")
            dst = new
        taken[dst] = src
        final.append((src, dst))
    return final


def apply(plan):
    moved = []
    for src, dst in plan:
        if norm(src) == norm(dst):
            continue
        os.makedirs(os.path.dirname(dst), exist_ok=True)
        shutil.move(src, dst)
        if os.path.exists(src + ".meta"):
            shutil.move(src + ".meta", dst + ".meta")
        moved.append({"src": norm(src), "dst": norm(dst)})
    json.dump(moved, open(MAP_FILE, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    return moved


def rollback():
    moved = json.load(open(MAP_FILE, encoding="utf-8"))
    for m in reversed(moved):
        src, dst = m["src"], m["dst"]
        if not os.path.exists(dst):
            print(f"  [跳过] 目标不存在: {dst}")
            continue
        os.makedirs(os.path.dirname(src), exist_ok=True)
        shutil.move(dst, src)
        if os.path.exists(dst + ".meta"):
            shutil.move(dst + ".meta", src + ".meta")
    print(f"已回滚 {len(moved)} 个文件")


def list_empty():
    """列出迁移后变空的源目录（不删除，交由用户在 Unity 中清理）"""
    empty = []
    for root in ["Assets/Project Files/Game/Images", "Assets/Textures"]:
        if not os.path.isdir(root):
            continue
        for dp, dn, fn in os.walk(root, topdown=False):
            if not os.listdir(dp):
                empty.append(norm(dp))
    return empty


def main():
    if "--rollback" in sys.argv:
        rollback()
        return

    plan = build_plan()
    print(f"计划移动: {len(plan)} 个文件 → {DST_ROOT}/\n")

    groups = collections.defaultdict(int)
    for src, dst in plan:
        groups[dst[len(DST_ROOT) + 1:].rsplit("/", 1)[0]] += 1
    print("=== 目标分组 ===")
    for g, c in sorted(groups.items()):
        print(f"  {c:4d}  {g}/")

    if "--apply" not in sys.argv:
        print("\n[dry-run] 加 --apply 实际执行")
        return

    moved = apply(plan)
    print(f"\n已移动 {len(moved)} 个文件")
    empty = list_empty()
    print(f"迁移后变空的源目录 {len(empty)} 个（未删除，可在 Unity 中清理）:")
    for d in empty:
        print(f"  - {d}")


if __name__ == "__main__":
    main()
