# 关闭阻塞 Unity 主线程的模态弹窗（Watermelon 推广窗等）。
# 模态窗会卡住 EditorApplication.update，导致 Unity MCP 的 run_tests / execute_code 永远 pending。
# 用法: powershell -ExecutionPolicy Bypass -File Tools/dismiss_unity_popup.ps1 [-Match "promo pattern"]
param([string]$Match = "")

Add-Type @'
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
public class Win {
  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc cb, IntPtr l);
  public delegate bool EnumWindowsProc(IntPtr h, IntPtr l);
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowTextW(IntPtr h, StringBuilder s, int n);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("user32.dll")] public static extern IntPtr SendMessageW(IntPtr h, uint msg, IntPtr w, IntPtr l);
  public static List<IntPtr> Popups(uint pid, string mainTitle) {
    var res = new List<IntPtr>();
    EnumWindows((h, l) => {
      uint p; GetWindowThreadProcessId(h, out p);
      if (p == pid && IsWindowVisible(h)) {
        var sb = new StringBuilder(512); GetWindowTextW(h, sb, 512);
        string t = sb.ToString();
        if (t.Length > 0 && t != mainTitle) res.Add(h);
      }
      return true;
    }, IntPtr.Zero);
    return res;
  }
  public static string Title(IntPtr h) { var sb = new StringBuilder(512); GetWindowTextW(h, sb, 512); return sb.ToString(); }
  public static void Close(IntPtr h) { SendMessageW(h, 0x0010, IntPtr.Zero, IntPtr.Zero); }
}
'@

$p = Get-Process Unity -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $p) { Write-Output "NO_UNITY_RUNNING"; exit 1 }
$main = "cakehome"
foreach ($h in [Win]::Popups([uint32]$p.Id, $main)) {
  $t = [Win]::Title($h)
  if ($Match -ne "" -and $t -notmatch $Match) { Write-Output "SKIP: $t"; continue }
  [Win]::Close($h)
  Write-Output "CLOSED: $t"
}
