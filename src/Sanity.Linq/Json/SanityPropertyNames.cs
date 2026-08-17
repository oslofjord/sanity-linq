// Copywrite 2018 Oslofjord Operations AS

// This file is part of Sanity LINQ (https://github.com/oslofjord/sanity-linq).

//  Sanity LINQ is free software: you can redistribute it and/or modify
//  it under the terms of the MIT Licence.

//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//  MIT Licence for more details.

//  You should have received a copy of the MIT Licence
//  along with this program.

using Sanity.Linq.Extensions;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Sanity.Linq.Json
{
    /// <summary>
    /// Resolves the Sanity field name a CLR member maps to.
    ///
    /// Used by the query translator (to emit GROQ field names) and by the reference
    /// converter (to locate the _id field on a referenced document). It is deliberately
    /// separate from the serializer: GROQ generation has to agree with serialization, but it
    /// does not run through it.
    ///
    /// Two attributes are honoured:
    ///
    ///  - [JsonPropertyName] from System.Text.Json - the supported way from 2.0 onwards.
    ///  - [JsonProperty] from Newtonsoft.Json - recognised by name via reflection, so that
    ///    models written against 1.x keep producing correct queries without taking a
    ///    dependency on Newtonsoft.Json here. Serialization of those models additionally
    ///    requires the Sanity.Linq.Newtonsoft compatibility package.
    /// </summary>
    public static class SanityPropertyNames
    {
        private const string NewtonsoftPropertyAttribute = "Newtonsoft.Json.JsonPropertyAttribute";
        private const string NewtonsoftIgnoreAttribute = "Newtonsoft.Json.JsonIgnoreAttribute";

        private static readonly ConcurrentDictionary<MemberInfo, string> _explicitNames =
            new ConcurrentDictionary<MemberInfo, string>();

        private static readonly ConcurrentDictionary<Type, PropertyInfo> _newtonsoftNameAccessors =
            new ConcurrentDictionary<Type, PropertyInfo>();

        /// <summary>
        /// The field name explicitly configured on a member via attribute, or null when the
        /// member relies on the naming convention.
        /// </summary>
        public static string GetExplicitFieldName(MemberInfo member)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            // ConcurrentDictionary cannot cache nulls, so absence is cached as "".
            var cached = _explicitNames.GetOrAdd(member, m => ResolveExplicitFieldName(m) ?? "");
            return cached.Length == 0 ? null : cached;
        }

        /// <summary>
        /// The Sanity field name a member maps to: its explicit name when one is configured,
        /// otherwise the member name camel-cased by convention.
        /// </summary>
        public static string GetFieldName(MemberInfo member)
        {
            return GetExplicitFieldName(member) ?? member.Name.ToCamelCase();
        }

        /// <summary>
        /// Whether a member is excluded from serialization, and therefore also from GROQ
        /// projections.
        ///
        /// Recognises both System.Text.Json's [JsonIgnore] and, for models written against
        /// 1.x, Newtonsoft.Json's.
        /// </summary>
        public static bool IsIgnored(MemberInfo member)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            if (member.GetCustomAttribute<JsonIgnoreAttribute>(true) != null)
            {
                return true;
            }

            foreach (var attribute in member.GetCustomAttributes(true))
            {
                if (attribute.GetType().FullName == NewtonsoftIgnoreAttribute)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Finds the property representing a given Sanity system field (for example "_id").
        ///
        /// A property qualifies when it is literally named after the field (case-insensitively)
        /// or when it declares that field name explicitly by attribute.
        /// </summary>
        public static PropertyInfo FindPropertyForField(Type type, string fieldName)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return type.GetProperties().FirstOrDefault(p => RepresentsField(p, fieldName));
        }

        /// <summary>
        /// Whether a property maps to the given Sanity field name.
        /// </summary>
        public static bool RepresentsField(PropertyInfo property, string fieldName)
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            return property.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(GetExplicitFieldName(property), fieldName, StringComparison.Ordinal);
        }

        private static string ResolveExplicitFieldName(MemberInfo member)
        {
            var name = member.GetCustomAttribute<JsonPropertyNameAttribute>(true)?.Name;
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }

            return GetNewtonsoftPropertyName(member);
        }

        /// <summary>
        /// Reads Newtonsoft.Json's [JsonProperty(...)] name reflectively, matching the
        /// attribute by type name so that no reference to Newtonsoft.Json is needed.
        /// </summary>
        private static string GetNewtonsoftPropertyName(MemberInfo member)
        {
            foreach (var attribute in member.GetCustomAttributes(true))
            {
                var attributeType = attribute.GetType();
                if (attributeType.FullName != NewtonsoftPropertyAttribute)
                {
                    continue;
                }

                var accessor = _newtonsoftNameAccessors.GetOrAdd(attributeType, t => t.GetProperty("PropertyName"));
                var name = accessor?.GetValue(attribute) as string;
                if (!string.IsNullOrEmpty(name))
                {
                    return name;
                }
            }

            return null;
        }
    }
}
