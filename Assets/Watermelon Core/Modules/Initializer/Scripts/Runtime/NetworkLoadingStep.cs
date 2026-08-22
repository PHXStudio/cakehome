using System;
using System.Collections;
using UnityEngine;

namespace Watermelon
{
    [RequireComponent(typeof(Initializer))]
    public class NetworkLoadingStep : MonoBehaviour, ILoadingStep
    {
        private const string CHECK_URL = "https://google.com/";

        public string LoadingMessage => "Checking connection..";
        public string ErrorMessage => "Connection error";

        public IEnumerator Execute(Initializer initializer, Action<bool> onCompleted)
        {
            bool isConnected = false;

            NetworkConnection networkConnection = new NetworkConnection(CHECK_URL);
            yield return networkConnection.CheckConnection((state) => isConnected = state);

            onCompleted?.Invoke(isConnected);
        }
    }
}
