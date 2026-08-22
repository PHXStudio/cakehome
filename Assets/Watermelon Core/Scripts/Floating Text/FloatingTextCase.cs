using UnityEngine;

namespace Watermelon
{
    [System.Serializable]
    public class FloatingTextCase
    {
        [SerializeField] string name;
        public string Name => name;

        [SerializeField] FloatingTextBaseBehavior floatingTextBehavior;
        public FloatingTextBaseBehavior FloatingTextBehavior => floatingTextBehavior;

        [Tooltip("Parent for pooled instances. Required for UI-based behaviors so they render inside a Canvas; leave empty for world-space behaviors.")]
        [SerializeField] Transform objectsContainer;
        public Transform ObjectsContainer => objectsContainer;

        private Pool floatingTextPool;
        public Pool FloatingTextPool => floatingTextPool;

        public void Init()
        {
            floatingTextPool = objectsContainer != null
                ? new Pool(floatingTextBehavior.gameObject, objectsContainer)
                : new Pool(floatingTextBehavior.gameObject);
        }
    }
}