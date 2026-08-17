using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;
using Xunit.Sdk;

namespace Sanity.Linq.Tests.Golden
{
    /// <summary>
    /// JSON comparison for the golden suite.
    ///
    /// Mutation payloads are compared semantically rather than as exact strings: the order
    /// of keys within a JSON object carries no meaning to Sanity's API, and it is not
    /// something the library should be pinned to. Array order IS significant and is
    /// preserved.
    ///
    /// This helper deliberately uses System.Text.Json regardless of what the library itself
    /// serializes with, so the comparison is a fixed reference point across the migration.
    /// </summary>
    public static class JsonAssert
    {
        public static void Equivalent(string expectedJson, string actualJson)
        {
            var expected = Canonicalize(expectedJson, nameof(expectedJson));
            var actual = Canonicalize(actualJson, nameof(actualJson));

            if (expected != actual)
            {
                throw new XunitException(
                    "JSON payloads are not equivalent (compared with object keys sorted).\n" +
                    $"Expected:\n{expected}\n\nActual:\n{actual}\n\n" +
                    $"First difference at offset {FirstDifference(expected, actual)}.");
            }
        }

        /// <summary>
        /// Reserialize with object keys sorted recursively, so two payloads that differ only
        /// in property ordering render identically. Array element order is left untouched.
        /// </summary>
        public static string Canonicalize(string json, string paramName = "json")
        {
            JsonNode node;
            try
            {
                node = JsonNode.Parse(json);
            }
            catch (JsonException ex)
            {
                throw new ArgumentException($"{paramName} is not valid JSON: {ex.Message}\n{json}", paramName, ex);
            }

            var sb = new StringBuilder();
            Write(node, sb);
            return sb.ToString();
        }

        private static void Write(JsonNode node, StringBuilder sb)
        {
            switch (node)
            {
                case null:
                    sb.Append("null");
                    break;

                case JsonObject obj:
                    sb.Append('{');
                    var first = true;
                    foreach (var property in obj.OrderBy(p => p.Key, StringComparer.Ordinal))
                    {
                        if (!first) sb.Append(',');
                        first = false;
                        sb.Append(JsonSerializer.Serialize(property.Key)).Append(':');
                        Write(property.Value, sb);
                    }
                    sb.Append('}');
                    break;

                case JsonArray array:
                    sb.Append('[');
                    for (var i = 0; i < array.Count; i++)
                    {
                        if (i > 0) sb.Append(',');
                        Write(array[i], sb);
                    }
                    sb.Append(']');
                    break;

                default:
                    // Values (string / number / bool) render via their own JSON form.
                    sb.Append(node.ToJsonString());
                    break;
            }
        }

        private static int FirstDifference(string a, string b)
        {
            var max = Math.Min(a.Length, b.Length);
            for (var i = 0; i < max; i++)
            {
                if (a[i] != b[i]) return i;
            }
            return max;
        }
    }
}
