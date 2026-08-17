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

using Sanity.Linq.Json;
using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Sanity.Linq.Newtonsoft
{
    /// <summary>
    /// Makes System.Text.Json honour Newtonsoft.Json's [JsonProperty] and [JsonIgnore] on
    /// model classes, so documents written for Sanity LINQ 1.x serialize to the same field
    /// names under 2.x.
    ///
    /// [JsonPropertyName] takes precedence when a member carries both.
    /// </summary>
    public class NewtonsoftAttributeTypeInfoResolver : DefaultJsonTypeInfoResolver
    {
        public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            var typeInfo = base.GetTypeInfo(type, options);

            if (typeInfo.Kind != JsonTypeInfoKind.Object)
            {
                return typeInfo;
            }

            for (var i = typeInfo.Properties.Count - 1; i >= 0; i--)
            {
                var property = typeInfo.Properties[i];
                if (!(property.AttributeProvider is MemberInfo member))
                {
                    continue;
                }

                if (SanityPropertyNames.IsIgnored(member))
                {
                    typeInfo.Properties.RemoveAt(i);
                    continue;
                }

                // GetExplicitFieldName already prefers [JsonPropertyName] over [JsonProperty],
                // so assigning it unconditionally keeps that precedence.
                var explicitName = SanityPropertyNames.GetExplicitFieldName(member);
                if (explicitName != null)
                {
                    property.Name = explicitName;
                }
            }

            return typeInfo;
        }
    }
}
