using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// JSON (de)serialization for the Save system, backed by Newtonsoft.Json instead of Unity's <see cref="JsonUtility"/>.
    /// The contract resolver mirrors JsonUtility's field-selection rules (public fields and private fields marked
    /// <see cref="SerializeField"/>; skips properties and <see cref="NonSerializedAttribute"/> fields) so existing
    /// <see cref="ISaveObject"/> implementations serialize identically without any changes.
    /// </summary>
    public static class SaveJson
    {
        private static readonly JsonSerializerSettings Settings = new()
        {
            ContractResolver = new UnitySerializeFieldContractResolver(),
            // Allows deserializing types like SaveFileContainer that only expose a private parameterless
            // constructor for this purpose (their public constructor takes runtime-only arguments).
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
        };

        public static string ToJson(object obj) => JsonConvert.SerializeObject(obj, Settings);

        public static T FromJson<T>(string json) => JsonConvert.DeserializeObject<T>(json, Settings);

        /// <summary>Selects members the same way <see cref="JsonUtility"/> does: public fields and private fields marked <see cref="SerializeField"/>; skips properties and <see cref="NonSerializedAttribute"/> fields.</summary>
        private sealed class UnitySerializeFieldContractResolver : DefaultContractResolver
        {
            protected override List<MemberInfo> GetSerializableMembers(Type objectType)
            {
                List<MemberInfo> members = new();

                for (Type type = objectType; type != null && type != typeof(object); type = type.BaseType)
                {
                    foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                    {
                        if (field.IsDefined(typeof(NonSerializedAttribute), false))
                            continue;

                        if (field.IsPublic || field.IsDefined(typeof(SerializeField), false))
                            members.Add(field);
                    }
                }

                return members;
            }

            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                JsonProperty property = base.CreateProperty(member, memberSerialization);
                property.Writable = true;
                property.Readable = true;
                return property;
            }
        }
    }
}
