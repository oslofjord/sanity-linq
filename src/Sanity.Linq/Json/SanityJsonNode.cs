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

using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Sanity.Linq.Json
{
    /// <summary>
    /// Null-tolerant readers for JsonNode.
    ///
    /// Block content is loosely structured: any field may be absent, and reading a missing
    /// field should yield null rather than throw. Newtonsoft's JToken indexers and casts
    /// behaved that way; System.Text.Json's do not, so the leniency is provided here.
    /// </summary>
    public static class SanityJsonNode
    {
        /// <summary>
        /// The value of a named field as a string, or null when the field is absent, null,
        /// or not a scalar.
        /// </summary>
        public static string GetString(JsonNode node, string fieldName)
        {
            return ToStringValue(GetField(node, fieldName));
        }

        /// <summary>
        /// The value of a named field as an int, or null when absent or not numeric.
        /// </summary>
        public static int? GetInt(JsonNode node, string fieldName)
        {
            var field = GetField(node, fieldName);
            if (field is JsonValue value && value.GetValueKind() == JsonValueKind.Number)
            {
                // TryGetValue rather than GetValue: a non-integral number should read as
                // absent instead of throwing, matching how 1.x coerced these casts.
                if (value.TryGetValue<int>(out var number))
                {
                    return number;
                }

                if (value.TryGetValue<double>(out var asDouble))
                {
                    return (int)asDouble;
                }
            }
            return null;
        }

        /// <summary>
        /// The value of a named field as a bool, or null when absent or not a boolean.
        /// </summary>
        public static bool? GetBool(JsonNode node, string fieldName)
        {
            var field = GetField(node, fieldName);
            if (field is JsonValue value)
            {
                var kind = value.GetValueKind();
                if (kind == JsonValueKind.True || kind == JsonValueKind.False)
                {
                    return value.GetValue<bool>();
                }
            }
            return null;
        }

        /// <summary>
        /// A named field, or null when the node is null or is not an object.
        /// </summary>
        public static JsonNode GetField(JsonNode node, string fieldName)
        {
            return node is JsonObject obj && obj.TryGetPropertyValue(fieldName, out var value) ? value : null;
        }

        /// <summary>
        /// A named field as an array, or null when it is absent or not an array.
        /// </summary>
        public static JsonArray GetArray(JsonNode node, string fieldName)
        {
            return GetField(node, fieldName) as JsonArray;
        }

        /// <summary>
        /// Renders a node as text: the raw value for scalars, JSON for objects and arrays.
        /// Matches how 1.x interpolated JToken values into HTML.
        /// </summary>
        public static string ToStringValue(JsonNode node)
        {
            if (node == null)
            {
                return null;
            }

            if (node is JsonValue value)
            {
                switch (value.GetValueKind())
                {
                    case JsonValueKind.String:
                        return value.GetValue<string>();
                    case JsonValueKind.Null:
                        return null;
                    default:
                        return value.ToJsonString();
                }
            }

            return node.ToJsonString();
        }
    }
}
