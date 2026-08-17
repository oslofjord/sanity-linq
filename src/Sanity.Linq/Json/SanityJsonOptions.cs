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

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sanity.Linq.Json
{
    /// <summary>
    /// The serializer configuration Sanity LINQ uses by default.
    /// </summary>
    public static class SanityJsonOptions
    {
        /// <summary>
        /// Creates the default options: camelCased property and dictionary keys, nulls
        /// omitted, and Sanity reference handling.
        ///
        /// A new instance is returned per call because JsonSerializerOptions becomes
        /// read-only once used, and callers are free to adjust it before handing it over.
        /// </summary>
        public static JsonSerializerOptions CreateDefault()
        {
            return new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,

                // Sanity's localized types are dictionaries, and 1.x camel-cased their keys
                // as well as property names (Newtonsoft's CamelCasePropertyNamesContractResolver
                // did both). System.Text.Json needs this stated separately.
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,

                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,

                // Sanity documents routinely carry HTML and non-ASCII text. The default
                // encoder would escape <, >, & and everything above ASCII, changing the
                // bytes 1.x sent for the same content.
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,

                // Sanity returns camelCase; being lenient also matches Newtonsoft's
                // case-insensitive property matching in 1.x.
                PropertyNameCaseInsensitive = true,

                Converters = { new SanityReferenceTypeConverter() },
            };
        }
    }
}
