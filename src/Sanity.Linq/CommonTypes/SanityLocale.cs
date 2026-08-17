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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Sanity.Linq.CommonTypes
{
    public class SanityLocale<T> : Dictionary<string, object>
    {
        /// <summary>
        /// Options used to convert raw translation values into T.
        ///
        /// Translation values are stored as loose JSON, so converting one to T is a second,
        /// separate deserialization step that does not see the options the containing
        /// document was read with. 1.x had the same limitation (it went through
        /// Newtonsoft's global defaults); using the library defaults here keeps that
        /// behaviour, including case-insensitive property matching.
        /// </summary>
        private static readonly JsonSerializerOptions ConversionOptions = Json.SanityJsonOptions.CreateDefault();

        public SanityLocale()
        {
        }

        public SanityLocale(string sanityTypeName)
        {
            Type = sanityTypeName;
        }

        [JsonIgnore]
        public string Type
        {
            get => ContainsKey("_type") ? this["_type"]?.ToString() : null;
            set => this["_type"] = value;
        }
        

        public IReadOnlyDictionary<string, T> Translations =>
            this.Where(kv => kv.Key != "_type").ToDictionary(kv => kv.Key, kv => Convert(kv.Value));

        public T Get(string languageCode)
        {
            return ContainsKey(languageCode) ? Convert(this[languageCode]) : default(T);
        }

        /// <summary>
        /// Converts a raw dictionary value to T.
        ///
        /// Values set in code are already of type T. Values that came from a Sanity response
        /// arrive as whatever the deserializer produced for "object": JsonElement, or a
        /// JsonNode when the value was assembled through the DOM.
        /// </summary>
        private static T Convert(object value)
        {
            switch (value)
            {
                case null:
                    return default(T);

                case T typed:
                    return typed;

                case JsonElement element:
                    return FromJsonElement(element);

                case JsonNode node:
                    return node.Deserialize<T>(ConversionOptions);

                default:
                    return FromScalar(value.ToString());
            }
        }

        private static T FromJsonElement(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return default(T);

                case JsonValueKind.Object:
                case JsonValueKind.Array:
                    return element.Deserialize<T>(ConversionOptions);

                case JsonValueKind.String:
                    return FromScalar(element.GetString());

                default:
                    // Numbers and booleans: fall back to their JSON text so the same
                    // conversion rules apply as for strings.
                    return FromScalar(element.GetRawText());
            }
        }

        private static T FromScalar(string value)
        {
            if (value == null)
            {
                return default(T);
            }

            if (typeof(T) == typeof(string))
            {
                return (T)(object)value;
            }

            return (T)System.Convert.ChangeType(value, typeof(T));
        }

        public void Set(string languageCode, T value)
        {
            this[languageCode] = value;
        }

    }
}
