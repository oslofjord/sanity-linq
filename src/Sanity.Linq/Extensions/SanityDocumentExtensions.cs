// Copywrite 2018 Oslofjord Operations AS

// This file is part of Sanity LINQ (https://github.com/oslofjord/sanity-linq).

//  Sanity LINQ is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.

//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//  GNU General Public License for more details.

//  You should have received a copy of the GNU General Public License
//  along with this program.If not, see<https://www.gnu.org/licenses/>.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Sanity.Linq.BlockContent;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sanity.Linq.Extensions
{
    public static class SanityDocumentExtensions
    {

        /// <summary>
        /// Determines if object is a Sanity draft document by inspecting the Id field.
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        public static bool IsDraft(this object document)
        {
            if (document == null) return false;
            var id = document.SanityId();
            if (id == null) return false;
            return id.StartsWith("drafts.");
        }

        public static bool IsDefined(this object property)
        {
            return property != null;
        }


        /// <summary>
        /// Returns Type of a document using reflection to find a field which serializes to "_id"
        /// </summary>
        /// <param name="document">Object which is expected to represent a single Sanity document.</param>
        /// <returns></returns>
        public static string SanityId(this object document)
        {
            if (document == null) return null;

            // Return Id using reflection (based on conventions)
            var idProperty = document.GetType().GetIdProperty();
            if (idProperty != null)
            {
                return idProperty.GetValue(document)?.ToString();
            }

            // ID not found
            return null;
        }

        public static void SetSanityId(this object document, string value)
        {
            if (document == null) return;

            // Return Id using reflection (based on conventions)
            var idProperty = document.GetType().GetIdProperty();
            if (idProperty != null)
            {
                idProperty.SetValue(document, value);
            }
        }

        /// <summary>
        /// Returns Type of a document using reflection to find a field which serializes to "_type"
        /// </summary>
        /// <param name="document">Object which is expected to represent a single Sanity document.</param>
        /// <returns></returns>
        public static string SanityType(this object document)
        {
            if (document == null) return null;

            // Return type using reflection (based on conventions)
            var docTypeProperty = document.GetType().GetTypeProperty();
            if (docTypeProperty != null)
            {
                return docTypeProperty.GetValue(document)?.ToString();
            }
            return null;
        }

        /// <summary>
        /// Returns Type of a document using reflection to find a field which serializes to "_rev"
        /// </summary>
        /// <param name="document">Object which is expected to represent a single Sanity document.</param>
        /// <returns></returns>
        public static string SanityRevision(this object document)
        {
            if (document == null) return null;

            // Return type using reflection (based on conventions)
            var revisionProperty = document.GetType().GetRevisionProperty();
            if (revisionProperty != null)
            {
                return revisionProperty.GetValue(document)?.ToString();
            }
            return null;
        }


        public static DateTimeOffset? SanityCreatedAt(this object document)
        {
            if (document == null) return null;

            // Return type using reflection (based on conventions)
            var revisionProperty = document.GetType().GetCreatedAtProperty();
            if (revisionProperty != null)
            {
                var val = revisionProperty.GetValue(document);
                return Convert.ChangeType(val, typeof(DateTimeOffset)) as DateTimeOffset?;
            }
            return null;
        }


        public static DateTimeOffset? SanityUpdatedAt(this object document)
        {
            if (document == null) return null;

            // Return type using reflection (based on conventions)
            var revisionProperty = document.GetType().GetUpdatedAtProperty();
            if (revisionProperty != null)
            {
                var val = revisionProperty.GetValue(document);
                return Convert.ChangeType(val, typeof(DateTimeOffset)) as DateTimeOffset?;
            }
            return null;
        }

        /// <summary>
        /// Indicates that documents has a Sanity _type field
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        internal static bool HasDocumentTypeProperty(this object document)
        {
            return document?.GetType()?.GetTypeProperty() != null;
        }

        /// <summary>
        /// Indicates that documents has a Sanity _id field
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        internal static bool HasIdProperty(this object document)
        {
            return document?.GetType()?.GetIdProperty() != null;
        }

        /// <summary>
        /// Indicates that documents has a Sanity _id field
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        internal static bool HasRevisionProperty(this object document)
        {
            return document?.GetType()?.GetRevisionProperty() != null;
        }

        public static object GetValue(this object document, string fieldName)
        {
            var prop = document.GetType().GetProperty(fieldName);
            if (prop != null && prop.CanRead)
            {
                return prop.GetValue(document);
            }
            var field = document.GetType().GetField(fieldName);
            if (field != null && field.IsPublic)
            {
                return field.GetValue(document);
            }
            return null;
        }

        public static T GetValue<T>(this object document, string fieldName)
        {
            var val = GetValue(document, fieldName);
            if (val != null)
            {
                var converted = Convert.ChangeType(val, typeof(T));
                if (converted != null)
                {
                    return (T)converted;
                }
            }
            return default(T);
        }

        private static ConcurrentDictionary<Type, PropertyInfo> _idPropertyCache = new ConcurrentDictionary<Type, PropertyInfo>();
        private static PropertyInfo GetIdProperty(this Type type)
        {
            if (!_idPropertyCache.ContainsKey(type))
            {
                var props = type.GetProperties();
                var idProperty = props.FirstOrDefault(p => p.Name.Equals("_id", StringComparison.InvariantCultureIgnoreCase) ||
                                           (p.GetCustomAttribute<JsonPropertyAttribute>(true) != null && p.GetCustomAttribute<JsonPropertyAttribute>(true).PropertyName == "_id"));
                _idPropertyCache[type] = idProperty;
            }
            return _idPropertyCache[type];
        }


        private static ConcurrentDictionary<Type, PropertyInfo> _typePropertyCache = new ConcurrentDictionary<Type, PropertyInfo>();
        private static PropertyInfo GetTypeProperty(this Type type)
        {
            if (!_typePropertyCache.ContainsKey(type))
            {
                var props = type.GetProperties();
                var typeProperty = props.FirstOrDefault(p => p.Name.Equals("_type", StringComparison.InvariantCultureIgnoreCase) ||
                                                           (p.GetCustomAttribute<JsonPropertyAttribute>(true) != null && p.GetCustomAttribute<JsonPropertyAttribute>(true).PropertyName == "_type"));

                _typePropertyCache[type] = typeProperty;
            }
            return _typePropertyCache[type];
        }


        private static ConcurrentDictionary<Type, PropertyInfo> _revPropertyCache = new ConcurrentDictionary<Type, PropertyInfo>();
        private static PropertyInfo GetRevisionProperty(this Type type)
        {
            if (!_revPropertyCache.ContainsKey(type))
            {
                var props = type.GetProperties();
                var revProperty = props.FirstOrDefault(p => p.Name.Equals("_rev", StringComparison.InvariantCultureIgnoreCase) ||
                                                           (p.GetCustomAttribute<JsonPropertyAttribute>(true) != null && p.GetCustomAttribute<JsonPropertyAttribute>(true).PropertyName == "_rev"));

                _revPropertyCache[type] = revProperty;
            }
            return _revPropertyCache[type];
        }


        private static ConcurrentDictionary<Type, PropertyInfo> _createdAtPropertyCache = new ConcurrentDictionary<Type, PropertyInfo>();
        private static PropertyInfo GetCreatedAtProperty(this Type type)
        {
            if (!_createdAtPropertyCache.ContainsKey(type))
            {
                var props = type.GetProperties();
                var revProperty = props.FirstOrDefault(p => p.Name.Equals("_createdAt", StringComparison.InvariantCultureIgnoreCase) ||
                                                           (p.GetCustomAttribute<JsonPropertyAttribute>(true) != null && p.GetCustomAttribute<JsonPropertyAttribute>(true).PropertyName == "_createdAt"));

                _createdAtPropertyCache[type] = revProperty;
            }
            return _createdAtPropertyCache[type];
        }

        private static ConcurrentDictionary<Type, PropertyInfo> _updatedAtPropertyCache = new ConcurrentDictionary<Type, PropertyInfo>();
        private static PropertyInfo GetUpdatedAtProperty(this Type type)
        {
            if (!_updatedAtPropertyCache.ContainsKey(type))
            {
                var props = type.GetProperties();
                var revProperty = props.FirstOrDefault(p => p.Name.Equals("_updatedAt", StringComparison.InvariantCultureIgnoreCase) ||
                                                           (p.GetCustomAttribute<JsonPropertyAttribute>(true) != null && p.GetCustomAttribute<JsonPropertyAttribute>(true).PropertyName == "_updatedAt"));

                _updatedAtPropertyCache[type] = revProperty;
            }
            return _updatedAtPropertyCache[type];
        }


        public static Task<string> ToHtmlAsync(this object blockContent, SanityHtmlBuilder builder)
        {
            return builder.BuildAsync(blockContent);
        }

        public static string ToHtml(this object blockContent, SanityHtmlBuilder builder)
        {
            return builder.BuildAsync(blockContent).Result;
        }

        public static Task<string> ToHtmlAsync(this object blockContent, SanityDataContext context)
        {
            return context.HtmlBuilder.BuildAsync(blockContent);
        }

        public static string ToHtml(this object blockContent, SanityDataContext context)
        {
            return context.HtmlBuilder.BuildAsync(blockContent).Result;
        }
    }
}
