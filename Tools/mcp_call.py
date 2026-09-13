#!/usr/bin/env python3
"""Minimal Unity MCP HTTP client for CLI use."""
import json, sys, urllib.request

URL = "http://127.0.0.1:8080/mcp"
_HDRS = {"Content-Type": "application/json", "Accept": "application/json, text/event-stream"}


def _post(body, sid=None):
    h = dict(_HDRS)
    if sid:
        h["mcp-session-id"] = sid
    req = urllib.request.Request(URL, data=json.dumps(body).encode(), headers=h, method="POST")
    with urllib.request.urlopen(req, timeout=600) as r:
        sid = r.headers.get("mcp-session-id", sid)
        raw = r.read().decode("utf-8", "replace")
    for line in raw.splitlines():
        if line.startswith("data: "):
            return json.loads(line[6:]), sid
    return json.loads(raw) if raw.strip() else {}, sid


def session():
    _, sid = _post({"jsonrpc": "2.0", "id": 1, "method": "initialize",
                    "params": {"protocolVersion": "2024-11-05", "capabilities": {},
                               "clientInfo": {"name": "cc-cli", "version": "1"}}})
    _post({"jsonrpc": "2.0", "method": "notifications/initialized"}, sid)
    return sid


def call(name, args=None, sid=None):
    sid = sid or session()
    res, _ = _post({"jsonrpc": "2.0", "id": 2, "method": "tools/call",
                    "params": {"name": name, "arguments": args or {}}}, sid)
    if "error" in res:
        return {"error": res["error"]}
    out = res.get("result", {})
    parts = out.get("content") or []
    txt = "\n".join(p.get("text", "") for p in parts if p.get("type") == "text")
    if out.get("structuredContent"):
        return out["structuredContent"]
    try:
        return json.loads(txt)
    except Exception:
        return txt


if __name__ == "__main__":
    tool = sys.argv[1]
    payload = json.loads(sys.argv[2]) if len(sys.argv) > 2 else {}
    print(json.dumps(call(tool, payload), ensure_ascii=False, indent=2))
