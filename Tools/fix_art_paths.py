# -*- coding: utf-8 -*-
"""按迁移映射表修复代码/脚本里硬编码的旧资源路径。

用法:
    python Tools/fix_art_paths.py            # dry-run
    python Tools/fix_art_paths.py --apply
"""
import os, json, sys

MAP_FILE = "Tools/art_migration_map.json"
# 需要检查硬编码路径的文件类型
SCAN_EXT = {'.cs', '.bat', '.py', '.md', '.json', '.sh'}
SKIP_DIRS = {'Library', 'Temp', 'obj', 'Logs', '.git', 'builds'}


def main():
    apply = "--apply" in sys.argv
    moved = json.load(open(MAP_FILE, encoding="utf-8"))
    # 只保留 src != dst 的
    pairs = [(m["src"], m["dst"]) for m in moved if m["src"] != m["dst"]]
    # 目录级映射（代码里引用目录而非具体文件的情况）
    pairs += [
        ("Assets/Project Files/Game/Images/Zone 1 Shop/zone1_generated",
         "Assets/Project Files/Game/Art/_Pending/Zone1Shop"),
        ("Assets/Project Files/Game/Images/Base/General UI",
         "Assets/Project Files/Game/Art/UI/Common"),
        ("Assets/Textures/CandyTiles",
         "Assets/Project Files/Game/Art/_Unused/CandyTiles"),
        ("Assets/HotUpdate/Scripts/Tutorial/Images",
         "Assets/Project Files/Game/Art/UI/Tutorial"),
    ]
    # 长路径优先，避免被短前缀提前吞掉
    pairs.sort(key=lambda x: -len(x[0]))
    print(f"映射条目: {len(pairs)}")

    hits = []
    for dp, dn, fn in os.walk("."):
        parts = set(dp.replace(os.sep, "/").split("/"))
        if parts & SKIP_DIRS:
            continue
        for f in fn:
            if os.path.splitext(f)[1].lower() not in SCAN_EXT:
                continue
            p = os.path.join(dp, f).replace(os.sep, "/")
            if p.startswith("./"):
                p = p[2:]
            if p.startswith("Tools/"):
                continue  # 迁移工具自身含旧路径字面量，跳过
            try:
                txt = open(p, encoding="utf-8", errors="ignore").read()
            except OSError:
                continue
            new = txt
            for src, dst in pairs:
                if src in new:
                    new = new.replace(src, dst)
            if new != txt:
                n = sum(1 for src, _ in pairs if src in txt)
                hits.append((p, n))
                if apply:
                    open(p, "w", encoding="utf-8").write(new)

    if not hits:
        print("未发现需要修复的硬编码路径")
        return
    print(f"\n{'已修复' if apply else '待修复'} {len(hits)} 个文件:")
    for p, n in hits:
        print(f"  {p}  ({n} 处)")
    if not apply:
        print("\n[dry-run] 加 --apply 实际执行")


if __name__ == "__main__":
    main()
