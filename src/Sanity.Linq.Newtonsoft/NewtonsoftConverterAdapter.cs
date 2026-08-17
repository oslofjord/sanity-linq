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
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using NewtonsoftJsonConverter = Newtonsoft.Json.JsonConverter;
using NewtonsoftJsonSerializer = Newtonsoft.Json.JsonSerializer;
using StjJsonConverter = System.Text.Json.Serialization.JsonConverter;

namespace Sanity.Linq.Newtonsoft
{
    /// <summary>
    /// Runs a Newtonsoft.Json converter inside System.Text.Json.
    ///
    /// The two reader/writer models are not compatible, so the value being converted is
    /// buffered and handed to the Newtonsoft converter through its own DOM. That means the
    /// conversion is not streaming, and the whole value is materialised - acceptable for a
    /// compatibility path, and invisible to the converter itself.
    /// </summary>
    public class NewtonsoftConverterAdapter : JsonConverterFactory
    {
        private readonly NewtonsoftJsonConverter _converter;
        private readonly NewtonsoftJsonSerializer _serializer;

        public NewtonsoftConverterAdapter(NewtonsoftJsonConverter converter, JsonSerializerSettings settings = null)
        {
            _converter = converter ?? throw new ArgumentNullException(nameof(converter));

            // The serializer handed to the converter must not contain the converter itself,
            // or a converter that calls back into the serializer would recurse.
            var innerSettings = settings == null ? new JsonSerializerSettings() : ShallowCopyWithoutConverters(settings);
            _serializer = NewtonsoftJsonSerializer.Create(innerSettings);
        }

        public override bool CanConvert(Type typeToConvert) => _converter.CanConvert(typeToConvert);

        public override StjJsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var adapterType = typeof(TypedAdapter<>).MakeGenericType(typeToConvert);
            return (StjJsonConverter)Activator.CreateInstance(adapterType, _converter, _serializer);
        }

        private static JsonSerializerSettings ShallowCopyWithoutConverters(JsonSerializerSettings settings)
        {
            return new JsonSerializerSettings
            {
                ContractResolver = settings.ContractResolver,
                NullValueHandling = settings.NullValueHandling,
                DefaultValueHandling = settings.DefaultValueHandling,
                DateFormatHandling = settings.DateFormatHandling,
                DateFormatString = settings.DateFormatString,
                DateTimeZoneHandling = settings.DateTimeZoneHandling,
                FloatParseHandling = settings.FloatParseHandling,
                MaxDepth = settings.MaxDepth,
            };
        }

        private class TypedAdapter<T> : System.Text.Json.Serialization.JsonConverter<T>
        {
            private readonly NewtonsoftJsonConverter _converter;
            private readonly NewtonsoftJsonSerializer _serializer;

            public TypedAdapter(NewtonsoftJsonConverter converter, NewtonsoftJsonSerializer serializer)
            {
                _converter = converter;
                _serializer = serializer;
            }

            public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                // Buffer the value, then replay it through a Newtonsoft reader.
                using (var document = JsonDocument.ParseValue(ref reader))
                {
                    var token = JToken.Parse(document.RootElement.GetRawText());
                    using (var tokenReader = new JTokenReader(token))
                    {
                        // Newtonsoft converters expect the reader to be positioned on the
                        // value's first token; JTokenReader starts before it.
                        tokenReader.Read();
                        return (T)_converter.ReadJson(tokenReader, typeToConvert, null, _serializer);
                    }
                }
            }

            public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            {
                var tokenWriter = new JTokenWriter();
                _converter.WriteJson(tokenWriter, value, _serializer);

                var token = tokenWriter.Token;
                if (token == null || token.Type == JTokenType.Null)
                {
                    writer.WriteNullValue();
                    return;
                }

                writer.WriteRawValue(token.ToString(Formatting.None));
            }
        }
    }
}
