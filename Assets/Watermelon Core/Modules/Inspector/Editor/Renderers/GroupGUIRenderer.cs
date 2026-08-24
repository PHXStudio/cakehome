using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    public sealed class GroupGUIRenderer : GUIRenderer
    {
        private List<GUIRenderer> renderers;
        private PropertyGrouper propertyGrouper;
        private GroupAttribute groupAttribute;
        private CustomInspector editor;
        private string effectiveID;

        public string GroupID => effectiveID;
        public string ParentPath { get; private set; }

        public GroupGUIRenderer(CustomInspector editor, GroupAttribute groupAttribute, List<GUIRenderer> renderers, string idPrefix = "")
        {
            this.editor = editor;
            this.renderers = renderers;
            this.groupAttribute = groupAttribute;

            effectiveID = idPrefix + groupAttribute.ID;
            string parentPath = PropertyUtility.GetSubstringBeforeLastSlash(groupAttribute.ID);
            ParentPath = string.IsNullOrEmpty(parentPath) ? "" : idPrefix + parentPath;

            propertyGrouper = CustomAttributesDatabase.GetGroupAttribute(groupAttribute.GetType());

            Order = groupAttribute.Order;

            IsVisible = renderers.IsAnyObjectVisible();

            if (groupAttribute.GetType() == typeof(BoxFoldoutAttribute))
            {
                BoxFoldoutAttribute foldoutAttribute = (BoxFoldoutAttribute)groupAttribute;

                // Override default state
                editor.GetFoldout(effectiveID, foldoutAttribute.DefaultState);
            }
        }

        public void AddRenderer(GUIRenderer renderer)
        {
            renderers.Add(renderer);
        }

        public override void OnGUI()
        {
            if (!IsVisible) return;

            propertyGrouper.BeginGroup(editor, effectiveID, groupAttribute.Label);

            if (propertyGrouper.DrawRenderers(editor, effectiveID))
            {
                foreach (GUIRenderer renderer in renderers)
                {
                    renderer.OnGUI();
                }
            }

            propertyGrouper.EndGroup();
        }

        public override void OnGUIChanged()
        {
            foreach (GUIRenderer renderer in renderers)
            {
                renderer.OnGUIChanged();
            }

            IsVisible = renderers.IsAnyObjectVisible();
        }
    }
}