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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Json.Nodes;

namespace Sanity.Linq.Newtonsoft
{
    /// <summary>
    /// Converts between the two JSON DOMs.
    ///
    /// Both are trees of the same JSON data, so the conversion goes via the JSON text. That
    /// costs a re-parse, which is acceptable on a compatibility path and keeps the mapping
    /// exact rather than reimplementing it node type by node type.
    /// </summary>
    internal static class JTokenBridge
    {
        public static JToken ToJToken(JsonNode node)
        {
            if (node == null)
            {
                return JValue.CreateNull();
            }

            return JToken.Parse(node.ToJsonString());
        }

        public static JsonNode ToJsonNode(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null)
            {
                return null;
            }

            return JsonNode.Parse(token.ToString(Formatting.None));
        }
    }
}
