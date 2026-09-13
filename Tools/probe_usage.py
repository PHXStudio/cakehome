# -*- coding: utf-8 -*-
"""运行时盘查工具：从跑着的游戏里查每个资源的真实用法。

用法: python Tools/probe_usage.py <探针名>
  探针: tiles | pieces | board | dock | powerups | ui | scene
"""
import json
import subprocess
import sys
import os

BRIDGE = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                      "agentbridge", "bridge.py")
PROJ = "D:/claudeWorkbase/cakehome"

PROBES = {
    # 三消棋盘：每格的 sprite 是什么、有几层、尺寸
    "board": r'''
var lc = UnityEngine.Object.FindObjectOfType<Watermelon.LevelController>();
if (lc == null) return "no LevelController";
var sb = new System.Text.StringBuilder();
var tiles = UnityEngine.Object.FindObjectsOfType<Watermelon.TileBehavior>();
sb.Append("TileBehavior count=" + tiles.Length + " || ");
for (int i = 0; i < tiles.Length && i < 6; i++) {
  var t = tiles[i];
  var sr = t.GetComponentInChildren<UnityEngine.SpriteRenderer>();
  sb.Append("[" + t.gameObject.name + " layer=" + t.transform.position.z);
  if (sr != null && sr.sprite != null) sb.Append(" sprite=" + sr.sprite.name + " size=" + sr.sprite.rect.width + "x" + sr.sprite.rect.height);
  else sb.Append(" sprite=none");
  var childs = t.GetComponentsInChildren<UnityEngine.SpriteRenderer>();
  sb.Append(" renderers=" + childs.Length);
  for (int j = 0; j < childs.Length; j++) {
    if (childs[j].sprite != null) sb.Append(" [" + j + "]" + childs[j].sprite.name);
  }
  sb.Append("] ");
}
return sb.ToString();
''',
    # 瓦片定义：每套 tile 的等级、可用关卡
    "tiles": r'''
var db = UnityEngine.Resources.Load<UnityEngine.ScriptableObject>("Level Database");
if (db == null) return "no db in Resources";
var f = db.GetType().GetField("tiles", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var arr = f.GetValue(db) as System.Array;
var sb = new System.Text.StringBuilder();
sb.Append("tiles=" + arr.Length + " || ");
foreach (var item in arr) {
  var t = item.GetType();
  var pf = t.GetField("prefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
  var af = t.GetField("availableFromLevel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
  var cf = t.GetField("collectionID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
  var p = pf.GetValue(item) as UnityEngine.GameObject;
  sb.Append(p.name + " fromLv=" + af.GetValue(item) + " coll=" + cf.GetValue(item) + " ; ");
}
return sb.ToString();
''',
    # Dock 槽位
    "dock": r'''
var d = UnityEngine.Object.FindObjectOfType<Watermelon.DockBehavior>();
if (d == null) return "no DockBehavior";
var sb = new System.Text.StringBuilder();
sb.Append("dock=" + d.gameObject.name + " || ");
var srs = d.GetComponentsInChildren<UnityEngine.SpriteRenderer>(true);
for (int i = 0; i < srs.Length; i++) {
  if (srs[i].sprite != null) sb.Append(srs[i].gameObject.name + "=" + srs[i].sprite.name + " ; ");
}
return sb.ToString();
''',
}


def call(action, code=None):
    cmd = [sys.executable, BRIDGE, "--project", PROJ]
    if code is None:
        cmd += ["send", action]
    else:
        cmd += ["send", action, "--arg", "code=" + code]
    r = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8",
                       errors="replace", timeout=180)
    return (r.stdout or "") + (r.stderr or "")


def main():
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    if len(sys.argv) < 2:
        print("可用探针:", ", ".join(PROBES))
        return
    name = sys.argv[1]
    if name not in PROBES:
        print("未知探针:", name, "| 可用:", ", ".join(PROBES))
        return
    # 用 MCP 的 execute_code 更灵活（支持多行 C#）
    mcp = os.path.join(os.path.dirname(os.path.abspath(__file__)), "mcp_call.py")
    payload = json.dumps({"action": "execute", "code": PROBES[name]})
    r = subprocess.run([sys.executable, mcp, "execute_code", payload],
                       capture_output=True, text=True, encoding="utf-8",
                       errors="replace", timeout=300)
    out = (r.stdout or "") + (r.stderr or "")
    try:
        d = json.loads(out)
        print(d.get("data", {}).get("result", out))
    except Exception:
        print(out[:3000])


if __name__ == "__main__":
    main()
