using System;
using UnityEngine;
using UnityEngine.Networking;

namespace Watermelon
{
    /// <summary>
    /// Example cloud save handler implementation for custom backend.
    /// Demonstrates how to implement ICloudSaveHandler for a REST API backend.
    ///
    /// Usage:
    /// 1. Extend SaveInitModule and override GetCloudHandler()
    /// 2. Create instance of CustomBackendCloudHandler with your API URL
    /// 3. Configure with user ID
    /// 4. Return from GetCloudHandler()
    /// </summary>
    public class CustomBackendCloudHandler : ICloudSaveHandler
    {
        private string apiBaseUrl;
        private string userId;
        private bool isAvailable = true;

        public bool IsAvailable => isAvailable;

        /// <summary>
        /// Create handler with custom backend configuration.
        /// Example: new CustomBackendCloudHandler("https://api.example.com/saves", userId)
        /// </summary>
        public CustomBackendCloudHandler(string baseUrl, string userIdentifier)
        {
            apiBaseUrl = baseUrl?.TrimEnd('/');
            userId = userIdentifier;

            if (string.IsNullOrEmpty(apiBaseUrl) || string.IsNullOrEmpty(userId))
            {
                Debug.LogError("[Cloud Save]: CustomBackendCloudHandler requires valid apiBaseUrl and userId");
                isAvailable = false;
            }
        }

        public void Init()
        {
            Debug.Log($"[Cloud Save]: CustomBackendCloudHandler initialized for user '{userId}' at {apiBaseUrl}");
        }

        /// <summary>
        /// Download save file from cloud.
        /// GET /api/saves/{userId}/{fileName}
        /// Response: { "json": "...", "metadata": { "timestampUnix": 0, "saveCount": 0 } }
        /// </summary>
        public void Download(string fileName, Action<bool, string, SaveFileMetadata> onComplete)
        {
            if (!isAvailable)
            {
                onComplete?.Invoke(false, null, null);
                return;
            }

            string url = $"{apiBaseUrl}/{userId}/{fileName}";

            var request = UnityWebRequest.Get(url);
            request.timeout = 10;

            var asyncOp = request.SendWebRequest();
            asyncOp.completed += _ => HandleDownloadResponse(request, fileName, onComplete);
        }

        private void HandleDownloadResponse(UnityWebRequest request, string fileName, Action<bool, string, SaveFileMetadata> onComplete)
        {
            try
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string responseText = request.downloadHandler.text;

                    // Parse response JSON
                    // Expected format: { "json": "...", "metadata": { "timestampUnix": 0, "saveCount": 0 } }
                    try
                    {
                        var response = JsonUtility.FromJson<CloudSaveResponse>(responseText);

                        if (response != null && !string.IsNullOrEmpty(response.json))
                        {
                            SaveFileMetadata metadata = response.metadata;
                            Debug.Log($"[Cloud Save]: Downloaded '{fileName}' from cloud - {metadata}");
                            onComplete?.Invoke(true, response.json, metadata);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[Cloud Save]: Failed to parse cloud response: {ex.Message}");
                    }

                    onComplete?.Invoke(false, null, null);
                }
                else if (request.responseCode == 404)
                {
                    // File doesn't exist in cloud - this is not an error
                    Debug.Log($"[Cloud Save]: No cloud save found for '{fileName}'");
                    onComplete?.Invoke(false, null, null);
                }
                else
                {
                    Debug.LogWarning($"[Cloud Save]: Download failed - HTTP {request.responseCode}: {request.error}");
                    onComplete?.Invoke(false, null, null);
                }
            }
            finally
            {
                request?.Dispose();
            }
        }

        /// <summary>
        /// Upload save file to cloud.
        /// POST /api/saves/{userId}/{fileName}
        /// Body: { "json": "...", "metadata": { "timestampUnix": 0, "saveCount": 0 } }
        /// </summary>
        public void Upload(string fileName, string json, SaveFileMetadata metadata, Action<bool> onComplete)
        {
            if (!isAvailable)
            {
                onComplete?.Invoke(false);
                return;
            }

            string url = $"{apiBaseUrl}/{userId}/{fileName}";

            var payload = new CloudSavePayload { json = json, metadata = metadata };
            string payloadJson = JsonUtility.ToJson(payload);

            var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(payloadJson));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 10;

            var asyncOp = request.SendWebRequest();
            asyncOp.completed += _ => HandleUploadResponse(request, fileName, onComplete);
        }

        private void HandleUploadResponse(UnityWebRequest request, string fileName, Action<bool> onComplete)
        {
            try
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[Cloud Save]: Uploaded '{fileName}' to cloud successfully");
                    onComplete?.Invoke(true);
                }
                else
                {
                    Debug.LogWarning($"[Cloud Save]: Upload failed - HTTP {request.responseCode}: {request.error}");
                    onComplete?.Invoke(false);
                }
            }
            finally
            {
                request?.Dispose();
            }
        }

        /// <summary>
        /// Delete save file from cloud.
        /// DELETE /api/saves/{userId}/{fileName}
        /// </summary>
        public void Delete(string fileName, Action<bool> onComplete)
        {
            if (!isAvailable)
            {
                onComplete?.Invoke(false);
                return;
            }

            string url = $"{apiBaseUrl}/{userId}/{fileName}";

            var request = UnityWebRequest.Delete(url);
            request.timeout = 10;

            var asyncOp = request.SendWebRequest();
            asyncOp.completed += _ => HandleDeleteResponse(request, fileName, onComplete);
        }

        private void HandleDeleteResponse(UnityWebRequest request, string fileName, Action<bool> onComplete)
        {
            try
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[Cloud Save]: Deleted '{fileName}' from cloud");
                    onComplete?.Invoke(true);
                }
                else
                {
                    Debug.LogWarning($"[Cloud Save]: Delete failed - HTTP {request.responseCode}: {request.error}");
                    onComplete?.Invoke(false);
                }
            }
            finally
            {
                request?.Dispose();
            }
        }

        // Helper classes for JSON serialization

        [System.Serializable]
        private class CloudSavePayload
        {
            public string json;
            public SaveFileMetadata metadata;
        }

        [System.Serializable]
        private class CloudSaveResponse
        {
            public string json;
            public SaveFileMetadata metadata;
        }
    }
}
