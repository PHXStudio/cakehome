# -*- coding: utf-8 -*-
"""扫描 CakeHome 项目美术资源的使用情况（基于 GUID 引用）。

- 美术资源收集范围: Assets/ 下排除第三方框架目录
- 引用来源扫描范围: Assets/ 全部 + Packages/ + ProjectSettings/（含框架目录，避免漏判孤儿）
输出: Tools/art_refs.json
"""
import os, re, json, collections

ART_ROOTS = ["Assets"]
# 这些目录下的美术资源不属于项目自有资产，不纳入归集
SKIP_ART_PARTS = {'Watermelon Core', 'TextMesh Pro', 'HybridCLRGenerate'}
ART_EXT = {'.png', '.jpg', '.jpeg', '.tga', '.psd'}
REF_EXT = {'.unity', '.prefab', '.asset', '.mat', '.controller', '.anim',
           '.playable', '.spriteatlas', '.json', '.cs', '.txt', '.xml'}
# 引用来源全量扫描（含框架、Packages）
REF_SCAN_DIRS = ["Assets", "Packages", "ProjectSettings"]


def norm(p):
    return p.replace(os.sep, '/')


def collect_art():
    """收集项目自有美术资源 guid -> path"""
    guid2path = {}
    for root in ART_ROOTS:
        for dp, dn, fn in os.walk(root):
            if any(s in norm(dp).split('/') for s in SKIP_ART_PARTS):
                continue
            for f in fn:
                if not f.endswith('.meta'):
                    continue
                owner = os.path.join(dp, f[:-5])
                if os.path.splitext(owner)[1].lower() not in ART_EXT:
                    continue
                if not os.path.exists(owner):
                    continue
                try:
                    head = open(os.path.join(dp, f), encoding='utf-8', errors='ignore').read(600)
                except OSError:
                    continue
                m = re.search(r'guid:\s*([0-9a-f]{32})', head)
                if m:
                    guid2path[m.group(1)] = norm(owner)
    return guid2path


def collect_refblobs():
    out = []
    for root in REF_SCAN_DIRS:
        if not os.path.isdir(root):
            continue
        for dp, dn, fn in os.walk(root):
            for f in fn:
                if os.path.splitext(f)[1].lower() not in REF_EXT:
                    continue
                p = os.path.join(dp, f)
                try:
                    out.append((norm(p), open(p, encoding='utf-8', errors='ignore').read()))
                except OSError:
                    pass
    return out


def main():
    guid2path = collect_art()
    refblobs = collect_refblobs()

    used = collections.defaultdict(list)
    for g, path in guid2path.items():
        for rp, blob in refblobs:
            if g in blob:
                used[path].append(rp)

    used_paths = set(used)
    all_paths = set(guid2path.values())
    orphans = sorted(all_paths - used_paths)

    report = {
        "total": len(all_paths),
        "used_count": len(used_paths),
        "orphan_count": len(orphans),
        "used": {p: sorted(set(used[p]))[:20] for p in sorted(used_paths)},
        "orphans": orphans,
    }
    with open("Tools/art_refs.json", "w", encoding="utf-8") as fp:
        json.dump(report, fp, ensure_ascii=False, indent=1)

    def dump(title, paths):
        print(f"\n=== {title} ({len(paths)}) ===")
        cnt = collections.Counter(os.path.dirname(p) for p in paths)
        for d, c in cnt.most_common():
            print(f"  {c:4d}  {d}")

    print(f"美术资源总数 : {len(all_paths)}")
    print(f"使用中       : {len(used_paths)}")
    print(f"未被引用     : {len(orphans)}")
    dump("使用中 - 按目录", sorted(used_paths))
    dump("未被引用 - 按目录", orphans)


if __name__ == '__main__':
    main()
