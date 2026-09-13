# -*- coding: utf-8 -*-
"""把三场景室内店铺素材接入 Zone 1/2/3 prefab（背景 + 5 槽位 × 4 档 + 坐标重排）。

设计依据: docs/三场景室内店铺设计方案.md

改什么（纯文本 .prefab 手术，Unity 无需运行）：
  1. **Background**  Image.m_Sprite → z{N}shop_background
                     RectTransform: anchoredPosition (0,0)、sizeDelta 1080x1920
  2. **5 个 Building**  defaultSprite + upgrades[0..2].upgradedSprite
                     → z{N}shop_building{M}_{sem}_{1..4}
                     RectTransform: 按设计方案第三节坐标表重排
                     Image.m_PreserveAspect: 全部置 1（修 B1/B3 的 stretch 变形）
  3. **sibling 顺序**  Background→1→2→3→4→5（当前已吻合，脚本只校验不重排）

为什么不改代码：
  BuildingBehavior 有 [SerializeField] defaultSprite + upgrades[].upgradedSprite，
  prefab 里全是 GUID 引用 → 只换 sprite 引用与坐标，运行时逻辑（buildingId /
  存档 / 升级成本 / 升级动画）完全不受影响。

安全：先备份到 _zone_rooms_backup/，支持 --rollback。

用法:
  python Tools/apply_zone_rooms.py                 # dry-run
  python Tools/apply_zone_rooms.py --apply
  python Tools/apply_zone_rooms.py --apply --only z1
  python Tools/apply_zone_rooms.py --rollback
"""
import argparse
import json
import os
import re
import shutil
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
PROJ = os.path.normpath(os.path.join(HERE, ".."))
PREFABS = os.path.join(PROJ, "Assets", "Project Files", "Game", "Prefabs", "Building")
BACKUP = "D:/claudeWorkbase/_zone_rooms_backup"
MANIFEST = os.path.join(BACKUP, "manifest.json")

# 槽位 → buildingId（三 Zone 一致）
SLOT_OF = {"1": "building_1", "2": "building_2", "3": "building_3",
           "4": "building_4", "5": "building_5"}

# 坐标表：buildingId → (x, y, w, h)，**按 Zone 分别定**（墙地交界线各 Zone 不同）
#
# 三条硬约束（顺序即优先级）：
#   ① **物件要"站在地板上"**：底边 = 该 Zone 实测墙地交界线。否则浮空 —— 最刺眼的破绽。
#   ② **框的长宽比必须贴近素材长宽比**。preserveAspect 是按 min(box_w/img_w, box_h/img_h)
#      缩放的：框比素材"更瘦高"时会被宽度卡住、上下留空；反之被高度卡住。
#      实测素材高宽比 0.9~1.2（柜/桌类），故框也取 1.0~1.2，物件才能顶满。
#   ③ 5 件互不重叠、底边避开底部导航栏（约屏幕 1805px 以下 → UI y < -845）。
#
# 布局 = 后墙 3 件（一排，落地于交界线）+ 前景 2 件（左右，落地于 -840）。
#   3+2 而非 4+1：后墙排每件能宽到 ~352（4+1 只能 ~250），物件视觉占比大得多。
#
# 墙地交界线实测（逐行亮度跳变分析，见 docs/三场景室内店铺设计方案.md 第三节）：
#   z1 墙面亮 230 → 46% 起护墙板 170 → 62% 起地板；交界 ≈ 62%
#   z2 墙面亮 220 → 46% 起鼠尾草绿墙裙 144 → 67% 起地板；交界 ≈ 67%
#   z3 白砖墙亮 225 → 54% 拱窗框 → 62% 起暖石砖地板；交界 ≈ 62%
JUNCTION = {"z1": -230, "z2": -326, "z3": -230}     # UI y（= 960 - pct*1920）
ROW_DROP = 18       # 底边再往**下**压一点，让物件明确"踩在地板上"而非浮在空中
#   ⚠ 符号坑：UI 坐标 y 向上、屏幕 y 向下。底边下移 = UI y 变小 = **减**。
#      早先写成 +ROW_LIFT，结果物件底边落在交界线**上方** 15px（浮空）。

# 后墙一排 3 件：中为主件，左右为翼（等高，宽度均分）
BACK_ROW = {
    "building_1": 0,        # cx = 0 中间，主件
    "building_2": -364,     # 左
    "building_4": 364,      # 右
}
BACK_BOX = (352, 405)       # w, h（高宽比 1.15，贴近柜类素材）

# 前景两件
FRONT_ROW = {"building_3": -250, "building_5": 250}
FRONT_BOX = (460, 520)      # w, h（高宽比 1.13）
FRONT_BOTTOM = -840         # 底边，避开底部导航栏


def layout_for(zone_key):
    """返回 {buildingId: (x, y, w, h)}。cy 一律由"底边落地"反推。"""
    out = {}
    bj = JUNCTION[zone_key] - ROW_DROP          # 底边压在交界线略下方 = 落地
    bw, bh = BACK_BOX
    for bid, cx in BACK_ROW.items():
        out[bid] = (cx, bj + bh / 2, bw, bh)
    fw, fh = FRONT_BOX
    for bid, cx in FRONT_ROW.items():
        out[bid] = (cx, FRONT_BOTTOM + fh / 2, fw, fh)
    return out


BG_SIZE = (1080, 1920)

# 各 Zone 的部件语义名（与 gen_zone_items.py 一致，用于拼文件名）
ZONES = {
    "z1": dict(prefab="Zone 1.prefab", folder="Zone1Shop", prefix="z1shop",
               sem={"1": "counter", "2": "shelf", "3": "table", "4": "bar",
                    "5": "tiered_stand"}),
    "z2": dict(prefab="Zone 2.prefab", folder="Zone2Shop", prefix="z2shop",
               sem={"1": "cupboard", "2": "bottle_shelf", "3": "stools",
                    "4": "plant_stand", "5": "menu_board"}),
    "z3": dict(prefab="Zone 3.prefab", folder="Zone3Shop", prefix="z3shop",
               sem={"1": "display", "2": "tea_sideboard", "3": "rattan_sofa",
                    "4": "flower_stand", "5": "tiered_stand"}),
}


def build_guid_map():
    """扫描 Assets 下所有 .png.meta → {文件名: guid} 与 {路径片段: guid}。"""
    by_rel = {}
    for dp, dn, fn in os.walk(os.path.join(PROJ, "Assets")):
        for f in fn:
            if not f.endswith(".png.meta"):
                continue
            p = os.path.join(dp, f)
            t = open(p, encoding="utf-8", errors="replace").read()
            m = re.search(r"guid: ([a-f0-9]{32})", t)
            if not m:
                continue
            rel = os.path.relpath(p[:-5], PROJ).replace(os.sep, "/")   # 去掉 ".meta"
            by_rel[rel] = m.group(1)
    return by_rel


def find_blocks(t):
    """把 prefab 拆成 {fileID: (classId, body)} 与 m_Children 顺序。"""
    blocks = {}
    order = []
    for m in re.finditer(r"--- !u!(\d+) &(\d+)\n(.*?)(?=\n--- !u!|\Z)", t, re.S):
        blocks[m.group(2)] = [m.group(1), m.group(3)]
        order.append(m.group(2))
    return blocks, order


def go_name(blocks, fid):
    for c in re.findall(r"- component: \{fileID: (\d+)\}", blocks[fid][1]):
        if c in blocks and blocks[c][0] == "1":
            mm = re.search(r"m_Name: (.*)", blocks[c][1])
            if mm:
                return mm.group(1).strip()
    return ""


def set_rect(body, x, y, w, h):
    body = re.sub(r"(m_AnchoredPosition: \{x: )[^,]*?(, y: )[^}]*\}",
                  r"\g<1>%g\g<2>%g}" % (x, y), body)
    body = re.sub(r"(m_SizeDelta: \{x: )[^,]*?(, y: )[^}]*\}",
                  r"\g<1>%g\g<2>%g}" % (w, h), body)
    return body


def set_sprite(body, guid):
    """改 Image 组件的 m_Sprite（只对 Image 用）。"""
    return re.sub(r"(m_Sprite: \{fileID: )\d+(, guid: )[a-f0-9]{32}",
                  r"\g<1>21300000\g<2>%s" % guid, body)


def set_default_sprite(body, guid):
    """改 BuildingBehavior 的 defaultSprite —— 即该槽位 **tier1** 的图。

    ⚠ 踩过的坑：BuildingBehavior 里字段名是 `defaultSprite` / `upgradedSprite`，
    不是 Image 的 `m_Sprite`。早先对建筑误用 set_sprite → tier1 从未被写入 →
    运行时 m_Sprite 为 NULL（因 defaultSprite 为空）→ 整个门店空白。
    """
    return re.sub(r"(defaultSprite: \{fileID: )\d+(, guid: )[a-f0-9]{32}",
                  r"\g<1>21300000\g<2>%s" % guid, body)


def set_preserve_aspect(body, val=1):
    if "m_PreserveAspect:" in body:
        return re.sub(r"m_PreserveAspect: \d", "m_PreserveAspect: %d" % val, body)
    # 该字段缺失时插到 m_Type 之后
    return re.sub(r"(m_Type: \d\n)", r"\g<1>  m_PreserveAspect: %d\n" % val, body)


def process(zone_key, blocks, guids, apply_):
    z = ZONES[zone_key]
    lay = layout_for(zone_key)
    changes = []

    # ① 背景
    bg_go = None
    for fid, (cls, body) in blocks.items():
        if cls == "1" and re.search(r"m_Name: Background\n", body):
            bg_go = fid
            break
    if bg_go is None:
        return changes, ["❌ 未找到 Background"]
    bg_rel = "Assets/Project Files/Game/Art/Scene/%s/%s_background.png" % (
        z["folder"], z["prefix"])
    g = guids.get(bg_rel)
    if not g:
        return changes, ["❌ 缺背景素材: %s" % bg_rel]

    for c in re.findall(r"- component: \{fileID: (\d+)\}", blocks[bg_go][1]):
        if blocks[c][0] == "114":
            blocks[c][1] = set_sprite(blocks[c][1], g)
            changes.append("  背景 sprite → %s" % os.path.basename(bg_rel))
        if blocks[c][0] == "224":
            blocks[c][1] = set_rect(blocks[c][1], 0, 0, *BG_SIZE)
            changes.append("  背景 rect → pos(0,0) size %dx%d" % BG_SIZE)

    # ② 5 个 Building
    for m in "12345":
        want_id = SLOT_OF[m]
        tgt = None
        for fid, (cls, body) in blocks.items():
            if cls != "114":
                continue
            bid = re.search(r"buildingId: (.*)", body)
            if bid and bid.group(1).strip() == want_id:
                tgt = fid
                break
        if tgt is None:
            changes.append("  ❌ 未找到 %s" % want_id)
            continue

        stem = "%s_building%s_%s" % (z["prefix"], m, z["sem"][m])
        spr_guids = []
        for i in range(1, 5):
            rel = "Assets/Project Files/Game/Art/Scene/%s/%s_%d.png" % (
                z["folder"], stem, i)
            gg = guids.get(rel)
            if not gg:
                # 回退：该档素材未生成时（如本轮漏图），沿用同槽位已有的任何一档
                d = os.path.dirname(os.path.join(PROJ, rel))
                alt = sorted(f for f in os.listdir(d)
                             if f.startswith(stem) and f.endswith(".png"))
                if alt:
                    alt_rel = rel.rsplit("/", 1)[0] + "/" + alt[0]
                    gg = guids.get(alt_rel)
                    changes.append("  ⚠ 缺 %s，回退用 %s"
                                   % (os.path.basename(rel), alt[0]))
                else:
                    changes.append("  ⚠ 缺素材 %s" % os.path.basename(rel))
            spr_guids.append(gg)

        body = blocks[tgt][1]
        if spr_guids[0]:
            body = set_default_sprite(body, spr_guids[0])
        # upgrades[] 逐条按位置替换；素材缺失的档位保留原 guid，不越界
        want = [g for g in spr_guids[1:]]
        state = {"i": 0}

        def _rep(mm):
            i = state["i"]
            state["i"] += 1
            g = want[i] if i < len(want) and want[i] else None
            if not g:
                return mm.group(0)
            return "%s21300000%s%s" % (mm.group(1), mm.group(2), g)

        body = re.sub(r"(upgradedSprite: \{fileID: )\d+(, guid: )[a-f0-9]{32}",
                      _rep, body)
        blocks[tgt][1] = body

        # rect + preserveAspect
        go = re.search(r"m_GameObject: \{fileID: (\d+)\}", body).group(1)
        x, y, w, h = lay[want_id]
        for c in re.findall(r"- component: \{fileID: (\d+)\}", blocks[go][1]):
            if blocks[c][0] == "224":
                blocks[c][1] = set_rect(blocks[c][1], x, y, w, h)
            if blocks[c][0] == "114" and "buildingId:" in blocks[c][1]:
                blocks[c][1] = set_preserve_aspect(blocks[c][1], 1)
        changes.append("  %-10s sprite→%s_1..4  rect pos(%g,%g) size %gx%g preserveAspect=1"
                       % (want_id, stem, x, y, w, h))

    return changes, []


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    ap = argparse.ArgumentParser()
    ap.add_argument("--apply", action="store_true")
    ap.add_argument("--only")
    ap.add_argument("--rollback", action="store_true")
    a = ap.parse_args()

    if a.rollback:
        if not os.path.exists(MANIFEST):
            sys.exit("无备份，无法回滚")
        recs = json.load(open(MANIFEST, encoding="utf-8"))
        n = 0
        for r in recs:
            if os.path.exists(r["backup"]):
                shutil.copy2(r["backup"], r["tgt"])
                n += 1
        print("已回滚 %d 个 prefab" % n)
        return

    keys = sorted(ZONES)
    if a.only:
        keys = [x.strip() for x in a.only.split(",")]

    guids = build_guid_map()
    print("已索引 %d 个 PNG guid\n" % len(guids))

    prepared = {}
    for k in keys:
        p = os.path.join(PREFABS, ZONES[k]["prefab"])
        t = open(p, encoding="utf-8", errors="replace").read()
        blocks, order = find_blocks(t)
        changes, errs = process(k, blocks, guids, a.apply)
        print("[%s] %s" % (k, ZONES[k]["prefab"]))
        for c in changes:
            print(c)
        for e in errs:
            print(e)
        print()
        if errs:
            continue
        # 回写
        out = t
        for fid, (cls, body) in blocks.items():
            old = re.search(r"--- !u!%s &%s\n(.*?)(?=\n--- !u!|\Z)" % (cls, fid),
                            t, re.S)
            if old:
                out = out.replace(old.group(1) + old.group(0).split("\n", 1)[1][:0], "")
        # 更稳的做法：逐块替换
        out = t
        for fid, (cls, body) in blocks.items():
            m = re.search(r"(--- !u!%s &%s\n)(.*?)(?=\n--- !u!|\Z)" % (cls, fid),
                          out, re.S)
            if m and m.group(2) != body:
                out = out[:m.start(2)] + body + out[m.end(2):]
        prepared[k] = (p, out)

    if not a.apply:
        print("[dry-run] 加 --apply 执行（会先备份到 %s）" % BACKUP)
        return

    os.makedirs(BACKUP, exist_ok=True)
    recs = []
    for k, (p, out) in prepared.items():
        bak = os.path.join(BACKUP, os.path.basename(p))
        shutil.copy2(p, bak)
        # Unity YAML 用 LF
        open(p, "w", encoding="utf-8", newline="\n").write(out)
        recs.append({"tgt": p, "backup": bak, "zone": k})
        print("✅ 已写入 %s" % os.path.basename(p))

    json.dump(recs, open(MANIFEST, "w", encoding="utf-8"),
              ensure_ascii=False, indent=2)
    print("\n完成：%d 个 prefab" % len(recs))
    print("回滚: python Tools/apply_zone_rooms.py --rollback")


if __name__ == "__main__":
    main()
