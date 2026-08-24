using System.Collections;
using System.Collections.Concurrent;
using UnityEngine;

namespace Watermelon
{
    public class MainThreadDispatcher
    {
        private readonly ConcurrentQueue<SimpleCallback> queue = new ConcurrentQueue<SimpleCallback>();
        private readonly bool async;

        public MainThreadDispatcher(MonoBehaviour host)
        {
            async = host != null;
            if (async) host.StartCoroutine(Process());
        }

        public void Dispatch(SimpleCallback callback)
        {
            if (async)
                queue.Enqueue(callback);
            else
                callback?.Invoke();
        }

        private IEnumerator Process()
        {
            while (true)
            {
                while (queue.TryDequeue(out var cb))
                {
                    try { cb?.Invoke(); }
                    catch (System.Exception e) { Debug.LogException(e); }
                }
                yield return null;
            }
        }
    }
}
