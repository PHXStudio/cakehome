using UnityEditor;

namespace Watermelon
{
    public abstract class EditorAdsContainer
    {
        protected SerializedProperty containerProperty;

        protected abstract string ContainerDisplayName { get; }

        public virtual void Init(SerializedProperty containerProp)
        {
            containerProperty = containerProp;
        }

        public virtual void DrawContainer()
        {
            containerProperty.isExpanded = EditorGUILayoutCustom.BeginExpandBoxGroup(ContainerDisplayName, containerProperty.isExpanded);

            if (containerProperty.isExpanded)
            {
                foreach (SerializedProperty prop in containerProperty.GetChildren())
                {
                    EditorGUILayout.PropertyField(prop);
                }

                SpecialButtons();
            }

            EditorGUILayoutCustom.EndBoxGroup();
        }

        protected abstract void SpecialButtons();
    }
}
