using UnityEngine;

namespace Watermelon
{
    [System.Serializable]
    public class MergeGradeData
    {
        [SerializeField] Sprite sprite;
        [SerializeField] string displayName;
        [SerializeField] string description;
        [SerializeReference] MergeGradeConfig config;

        [SerializeField] bool customShadow;
        [SerializeField] Sprite customShadowSprite;
        [SerializeField] Vector3 shadowPositionOffset;
        [SerializeField] Vector3 customShadowScale = Vector3.one;

        public Sprite Sprite => sprite;
        public string DisplayName => displayName;
        public string Description => description;
        public MergeGradeConfig Config => config;

        public bool CustomShadow => customShadow;
        public Sprite CustomShadowSprite => customShadowSprite;
        public Vector3 ShadowPositionOffset => shadowPositionOffset;
        public Vector3 CustomShadowScale => customShadowScale;

        public T GetConfig<T>() where T : MergeGradeConfig => config as T;

#if UNITY_EDITOR
        internal void SetConfigEditor(MergeGradeConfig value) => config = value;
#endif
    }
}
