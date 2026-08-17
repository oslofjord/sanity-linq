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
    /// Identifies types that model a free-form (schemaless) JSON value.
    ///
    /// The query translator must not walk into these when building a projection: their CLR
    /// members describe the DOM, not the Sanity document, so recursing would emit nonsense
    /// field names. A "{...}" projection is used instead.
    /// </summary>
    public static class SanityJsonTypes
    {
        // Recognised by name so that models written against 1.x continue to be treated as
        // free-form without this assembly referencing Newtonsoft.Json.
        private static readonly string[] NewtonsoftDomTypes =
        {
            "Newtonsoft.Json.Linq.JObject",
            "Newtonsoft.Json.Linq.JToken",
            "Newtonsoft.Json.Linq.JArray",
        };

        public static bool IsFreeFormJson(Type type)
        {
            if (type == null)
            {
                return false;
            }

            if (type == typeof(JsonObject)
                || type == typeof(JsonNode)
                || type == typeof(JsonArray)
                || type == typeof(JsonValue)
                || type == typeof(JsonElement)
                || type == typeof(JsonDocument))
            {
                return true;
            }

            return Array.IndexOf(NewtonsoftDomTypes, type.FullName) >= 0;
        }
    }
}
