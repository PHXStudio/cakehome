#!/usr/bin/env bash
# ============================================================
# Unity MCP 服务器 一键启动脚本
# 需要: uv (已安装), Unity 编辑器 (已装 com.coplaydev.unity-mcp 插件)
#
# 用法:
#   bash start-server.sh          # 前台启动
#   bash start-server.sh --bg     # 后台启动
#   bash start-server.sh --status # 查看状态
#   bash start-server.sh --stop   # 停止
# ============================================================
set -euo pipefail

PORT=8080
URL="http://127.0.0.1:${PORT}"
LOG="/tmp/mcp_unity_server.log"
PIDFILE="/tmp/mcp_unity_server.pid"

# 禁用 fastmcp 版本检查（代理 env 会触发 httpx bug）+ 清理 NO_PROXY
export FASTMCP_CHECK_FOR_UPDATES=off
export NO_PROXY="localhost,127.0.0.1"

start() {
  if curl -s --max-time 2 "${URL}/mcp" >/dev/null 2>&1; then
    echo "✓ MCP 服务器已在运行: ${URL}"
    return 0
  fi

  echo "启动 MCP 服务器 ${URL} ..."
  uvx --from mcpforunityserver mcp-for-unity \
    --transport http --http-url "${URL}" > "${LOG}" 2>&1 &
  echo $! > "${PIDFILE}"
  echo "PID: $(cat "${PIDFILE}")  (日志: ${LOG})"

  # 等待就绪
  for i in $(seq 1 30); do
    if curl -s --max-time 2 "${URL}/mcp" >/dev/null 2>&1; then
      echo "✓ 服务器已就绪"
      return 0
    fi
    sleep 1
  done
  echo "✗ 启动超时，查看日志: tail -20 ${LOG}"
  return 1
}

stop() {
  if [ -f "${PIDFILE}" ]; then
    kill "$(cat "${PIDFILE}")" 2>/dev/null || true
    rm -f "${PIDFILE}"
    echo "已停止 MCP 服务器"
  else
    echo "未找到 PID 文件"
  fi
}

status() {
  if curl -s --max-time 2 "${URL}/mcp" >/dev/null 2>&1; then
    echo "✓ MCP 服务器运行中: ${URL}"
    netstat -ano 2>/dev/null | grep ":${PORT} " | head -1 || true
  else
    echo "✗ MCP 服务器未运行"
  fi
}

case "${1:-}" in
  --bg)    start ;;
  --stop)  stop ;;
  --status) status ;;
  *)       start ;;
esac
