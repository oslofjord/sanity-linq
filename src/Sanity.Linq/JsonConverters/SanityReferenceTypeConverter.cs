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

using Sanity.Linq.CommonTypes;
using Sanity.Linq.Json;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Sanity.Linq
{
    /// <summary>
    /// Handles the two shapes a SanityReference&lt;T&gt; can arrive in.
    ///
    /// Reading:
    ///  - a real reference ({ "_ref": ... }) binds straight onto SanityReference&lt;T&gt;;
    ///  - a dereferenced document (the projection followed the reference, so the whole
    ///    document sits in the reference's place) is bound to Value, with Ref synthesised
    ///    from the document's _id.
    ///
    /// Writing: always emits a reference, deriving _ref from the nested document's _id when
    /// only Value was set.
    ///
    /// This is a factory because CanConvert has to match an open generic.
    /// </summary>
    public class SanityReferenceTypeConverter : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsGenericType
                && typeToConvert.GetGenericTypeDefinition() == typeof(SanityReference<>);
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var elementType = typeToConvert.GetGenericArguments()[0];
            var converterType = typeof(SanityReferenceConverter<>).MakeGenericType(elementType);
            return (JsonConverter)Activator.CreateInstance(converterType);
        }
    }

    /// <summary>
    /// The concrete converter behind <see cref="SanityReferenceTypeConverter"/>.
    /// </summary>
    internal class SanityReferenceConverter<T> : JsonConverter<SanityReference<T>> where T : class
    {
        private static readonly ConcurrentDictionary<Type, PropertyInfo> _idProperties =
            new ConcurrentDictionary<Type, PropertyInfo>();

        public override SanityReference<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            // The decision below depends on whether "_ref" is present, which is not known
            // until the object has been read, so the value is buffered into a DOM first.
            var node = JsonNode.Parse(ref reader);
            if (!(node is JsonObject obj))
            {
                // Unable to deserialize.
                return null;
            }

            // Built by hand rather than by re-entering the serializer: binding
            // SanityReference<T> while this converter is registered would recurse, and
            // deriving a converter-free JsonSerializerOptions per call would throw away
            // System.Text.Json's per-options metadata cache.
            //
            // Fields absent from the JSON are left at their constructed defaults, which is
            // how 1.x behaved (notably _key, which defaults to a new Guid).
            var result = new SanityReference<T>();

            if (obj.ContainsKey("_ref"))
            {
                // A plain reference.
                result.Ref = GetString(obj, "_ref");
                if (obj.ContainsKey("_type")) result.SanityType = GetString(obj, "_type");
            }
            else
            {
                // A dereferenced document occupying the reference's position: keep the whole
                // document and synthesise the reference from its _id.
                result.Ref = GetString(obj, "_id");
                result.SanityType = "reference";
                result.Value = obj.Deserialize<T>(options);
            }

            if (obj.ContainsKey("_key")) result.SanityKey = GetString(obj, "_key");
            if (obj.ContainsKey("_weak")) result.Weak = GetBool(obj, "_weak");

            return result;
        }

        public override void Write(Utf8JsonWriter writer, SanityReference<T> value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            var reference = value.Ref;

            // Fall back to the _id of the nested document when only Value was populated.
            if (string.IsNullOrEmpty(reference) && value.Value != null)
            {
                var idProperty = _idProperties.GetOrAdd(
                    value.Value.GetType(),
                    t => SanityPropertyNames.FindPropertyForField(t, "_id"));

                reference = idProperty?.GetValue(value.Value) as string;
            }

            if (string.IsNullOrEmpty(reference))
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            writer.WriteString("_ref", reference);
            writer.WriteString("_type", "reference");
            writer.WriteString("_key", string.IsNullOrEmpty(value.SanityKey) ? Guid.NewGuid().ToString() : value.SanityKey);
            if (value.Weak.HasValue)
            {
                writer.WriteBoolean("_weak", value.Weak.Value);
            }
            writer.WriteEndObject();
        }

        private static string GetString(JsonObject obj, string name)
        {
            return obj.TryGetPropertyValue(name, out var value) && value is JsonValue
                ? value.GetValue<string>()
                : null;
        }

        private static bool? GetBool(JsonObject obj, string name)
        {
            return obj.TryGetPropertyValue(name, out var value) && value is JsonValue
                ? value.GetValue<bool>()
                : (bool?)null;
        }
    }
}
