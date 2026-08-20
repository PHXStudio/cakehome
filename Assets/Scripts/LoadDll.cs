using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace Watermelon
{
    /// <summary>
    /// HybridCLR 热更新程序集加载器
    /// 开发阶段：从 StreamingAssets 加载 dll
    /// 生产阶段：从远程 CDN 下载并加载
    /// </summary>
    public class LoadDll : MonoBehaviour
    {
        [SerializeField] private bool loadFromRemote = false;
        [SerializeField] private string remoteBaseUrl = "https://your-cdn.com/hotupdate/";
        [SerializeField] private string[] aotDllNames = new string[]
        {
            "mscorlib.dll",
            "System.dll",
            "System.Core.dll",
        };

        [SerializeField] private string[] hotUpdateDllNames = new string[]
        {
            "HotUpdate.dll",
        };

        private IEnumerator Start()
        {
#if UNITY_EDITOR
            // 编辑器下 HotUpdate 已由 Unity 主编译加载（Library/ScriptAssemblies/HotUpdate.dll），
            // 不走 StreamingAssets 热更，避免程序集重复加载冲突。
            Debug.Log("[LoadDll] Editor mode: HotUpdate already compiled in, skipping hot reload.");
            yield break;
#else
            // 先加载 AOT 补充元数据（可选，用于解决 missing method 问题）
            if (aotDllNames != null && aotDllNames.Length > 0)
            {
                yield return LoadAOTAssemblies();
            }

            // 加载热更程序集
            if (hotUpdateDllNames != null && hotUpdateDllNames.Length > 0)
            {
                yield return LoadHotUpdateAssemblies();
            }

            // 调用热更入口
            InvokeHotUpdateEntry();
#endif
        }

        private IEnumerator LoadAOTAssemblies()
        {
            foreach (var dllName in aotDllNames)
            {
                byte[] dllBytes = null;
                if (loadFromRemote)
                {
                    yield return DownloadDll(dllName, (result) => dllBytes = result);
                }
                else
                {
                    dllBytes = LoadFromStreamingAssets(dllName);
                }

                if (dllBytes != null)
                {
                    // 加载 AOT 补充元数据
                    HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, HybridCLR.HomologousImageMode.SuperSet);
                    Debug.Log($"[LoadDll] AOT metadata loaded: {dllName}");
                }
            }
        }

        private IEnumerator LoadHotUpdateAssemblies()
        {
            foreach (var dllName in hotUpdateDllNames)
            {
                byte[] dllBytes = null;
                if (loadFromRemote)
                {
                    yield return DownloadDll(dllName, (result) => dllBytes = result);
                }
                else
                {
                    dllBytes = LoadFromStreamingAssets(dllName);
                }

                if (dllBytes != null)
                {
                    Assembly.Load(dllBytes);
                    Debug.Log($"[LoadDll] HotUpdate assembly loaded: {dllName}");
                }
            }
        }

        private byte[] LoadFromStreamingAssets(string dllName)
        {
            string path = Path.Combine(Application.streamingAssetsPath, "HotUpdate", dllName);
            if (File.Exists(path))
            {
                return File.ReadAllBytes(path);
            }
            Debug.LogWarning($"[LoadDll] File not found: {path}");
            return null;
        }

        private IEnumerator DownloadDll(string dllName, Action<byte[]> onComplete)
        {
            string url = remoteBaseUrl + dllName;
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                yield return www.SendWebRequest();
                if (www.result == UnityWebRequest.Result.Success)
                {
                    onComplete?.Invoke(www.downloadHandler.data);
                }
                else
                {
                    Debug.LogError($"[LoadDll] Download failed: {url} - {www.error}");
                    onComplete?.Invoke(null);
                }
            }
        }

        private void InvokeHotUpdateEntry()
        {
            var hotUpdateAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "HotUpdate");

            if (hotUpdateAssembly == null)
            {
                Debug.LogError("[LoadDll] HotUpdate assembly not found!");
                return;
            }

            var entryType = hotUpdateAssembly.GetType("HotUpdate.Entry");
            if (entryType == null)
            {
                Debug.LogError("[LoadDll] HotUpdate.Entry type not found in HotUpdate.dll!");
                return;
            }

            var method = entryType.GetMethod("Start", BindingFlags.Static | BindingFlags.Public);
            if (method != null)
            {
                method.Invoke(null, null);
                Debug.Log("[LoadDll] HotUpdate entry called successfully.");
            }
            else
            {
                Debug.LogWarning("[LoadDll] HotUpdate.Entry.Start() not found. Skipping.");
            }
        }
    }
}