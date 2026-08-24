using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Watermelon
{
    /// <summary>Shared editor utilities for RewardSetEditor and RewardsHolderEditor.</summary>
    internal static class RewardsEditorUtils
    {
        private static List<Type> cachedRewardTypes;

        /// <summary>Returns all concrete non-abstract Reward subclasses across all loaded assemblies, sorted by name.</summary>
        internal static List<Type> GetAllRewardTypes()
        {
            if (cachedRewardTypes != null)
                return cachedRewardTypes;

            cachedRewardTypes = new List<Type>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly == null) continue;

                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }

                foreach (Type t in types)
                {
                    if (t != null && t.IsClass && !t.IsAbstract && typeof(Reward).IsAssignableFrom(t))
                        cachedRewardTypes.Add(t);
                }
            }

            cachedRewardTypes = cachedRewardTypes.OrderBy(t => t.Name).ToList();
            return cachedRewardTypes;
        }

        /// <summary>Invalidates the cached type list so it is rebuilt on next access.</summary>
        internal static void InvalidateCache()
        {
            cachedRewardTypes = null;
            customDrawerTargets = null;
            hasCustomDrawerCache.Clear();
        }

        private static List<(Type targetType, bool useForChildren)> customDrawerTargets;
        private static readonly Dictionary<Type, bool> hasCustomDrawerCache = new Dictionary<Type, bool>();

        /// <summary>True if a [CustomPropertyDrawer] is registered for this exact type (or an assignable base with useForChildren).</summary>
        private static bool HasCustomPropertyDrawer(Type type)
        {
            if (type == null) return false;

            if (hasCustomDrawerCache.TryGetValue(type, out bool cached))
                return cached;

            if (customDrawerTargets == null)
                BuildCustomDrawerTargets();

            bool has = false;
            foreach (var (targetType, useForChildren) in customDrawerTargets)
            {
                if (targetType == type || (useForChildren && targetType.IsAssignableFrom(type)))
                {
                    has = true;
                    break;
                }
            }

            hasCustomDrawerCache[type] = has;
            return has;
        }

        // CustomPropertyDrawer doesn't expose its target type publicly — read the private
        // fields Unity itself uses internally to resolve drawers for [SerializeReference] values.
        private static void BuildCustomDrawerTargets()
        {
            customDrawerTargets = new List<(Type, bool)>();

            FieldInfo typeField = typeof(CustomPropertyDrawer).GetField("m_Type", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo useForChildrenField = typeof(CustomPropertyDrawer).GetField("m_UseForChildren", BindingFlags.NonPublic | BindingFlags.Instance);
            if (typeField == null || useForChildrenField == null) return;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly == null) continue;

                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }

                foreach (Type drawerType in types)
                {
                    if (drawerType == null || !typeof(PropertyDrawer).IsAssignableFrom(drawerType))
                        continue;

                    foreach (object attrObj in drawerType.GetCustomAttributes(typeof(CustomPropertyDrawer), true))
                    {
                        CustomPropertyDrawer attr = (CustomPropertyDrawer)attrObj;

                        if (!(typeField.GetValue(attr) is Type targetType)) continue;
                        bool useForChildren = (bool)useForChildrenField.GetValue(attr);

                        customDrawerTargets.Add((targetType, useForChildren));
                    }
                }
            }
        }

        /// <summary>Resolves the CLR Type from a SerializedProperty's managedReferenceFullTypename ("AssemblyName TypeFullName").</summary>
        internal static Type GetManagedReferenceSystemType(SerializedProperty prop)
        {
            if (prop == null) return null;

            string full = prop.managedReferenceFullTypename;
            if (string.IsNullOrEmpty(full)) return null;

            int space = full.IndexOf(' ');
            if (space < 0 || space + 1 >= full.Length) return null;

            string asmName = full.Substring(0, space);
            string typeName = full.Substring(space + 1);

            var asm = AppDomain.CurrentDomain
                               .GetAssemblies()
                               .FirstOrDefault(a => a.GetName().Name == asmName);
            if (asm != null)
            {
                var t = asm.GetType(typeName);
                if (t != null) return t;
            }

            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = a.GetType(typeName);
                if (t != null) return t;
            }

            return Type.GetType($"{typeName}, {asmName}");
        }

        /// <summary>Draws children of a managed-reference SerializedProperty without the root foldout.</summary>
        internal static void DrawManagedReferenceContents(SerializedProperty element)
        {
            if (element == null) return;

            // If the concrete runtime type has its own CustomPropertyDrawer, draw the element
            // itself so Unity resolves and invokes that drawer (it won't add its own foldout,
            // since a registered drawer fully owns the property's GUI). Falling into the generic
            // field-by-field loop below would only ever reach the plain child fields and skip it.
            Type concreteType = GetManagedReferenceSystemType(element);
            if (HasCustomPropertyDrawer(concreteType))
            {
                EditorGUILayout.PropertyField(element, GUIContent.none, includeChildren: true);
                return;
            }

            var copy = element.Copy();
            var end = copy.GetEndProperty();

            bool enterChildren = true;

            while (copy.NextVisible(enterChildren) && !SerializedProperty.EqualContents(copy, end))
            {
                if (copy.name == "m_Script")
                {
                    enterChildren = false;
                    continue;
                }

                EditorGUILayout.PropertyField(copy, includeChildren: true);
                enterChildren = false;
            }
        }

        /// <summary>Returns a human-readable type name from a managedReferenceFullTypename string.</summary>
        internal static string GetNiceTypeName(string managedReferenceFullTypename)
        {
            if (string.IsNullOrEmpty(managedReferenceFullTypename)) return "(null)";

            int space = managedReferenceFullTypename.IndexOf(' ');
            if (space >= 0 && space + 1 < managedReferenceFullTypename.Length)
            {
                string full = managedReferenceFullTypename.Substring(space + 1);
                int lastDot = full.LastIndexOf('.');
                return lastDot >= 0 ? full.Substring(lastDot + 1).AddSpaces() : full;
            }

            return managedReferenceFullTypename;
        }
    }
}
