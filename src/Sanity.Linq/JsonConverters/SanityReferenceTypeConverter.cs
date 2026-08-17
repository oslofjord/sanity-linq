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
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sanity.Linq.CommonTypes;

namespace Sanity.Linq
{
    public class SanityReferenceTypeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(SanityReference<>));
        }

        public override object ReadJson(JsonReader reader, Type type, object existingValue, JsonSerializer serializer)
        {
            var objectType = type;
            var elemType = objectType.GetGenericArguments()[0];
            var obj = serializer.Deserialize(reader) as JObject;
            if (obj != null)
            {
                if (obj.GetValue("_ref") != null)
                {
                    // Normal reference
                    return obj.ToObject(objectType);
                }
                else
                {
                    var res = Activator.CreateInstance(objectType);
                    objectType.GetProperty(nameof(SanityReference<object>.Ref)).SetValue(res, obj.GetValue("_id")?.ToString());
                    objectType.GetProperty(nameof(SanityReference<object>.SanityType)).SetValue(res, "reference");
                    objectType.GetProperty(nameof(SanityReference<object>.SanityKey)).SetValue(res, obj.GetValue("_key"));
                    objectType.GetProperty(nameof(SanityReference<object>.Weak)).SetValue(res, obj.GetValue("_weak"));
                    objectType.GetProperty(nameof(SanityReference<object>.Value)).SetValue(res, serializer.Deserialize(new StringReader(obj.ToString()), elemType));
                    return res;
                }
            }
            // Unable to deserialize
            return null;
        }

        public override void WriteJson(JsonWriter writer, object objectToSerialize, JsonSerializer serializer)
        {
            if (objectToSerialize != null)
            {
                var objectType = objectToSerialize.GetType();

                //Get reference from object
                var valRef = objectType.GetProperty("Ref").GetValue(objectToSerialize) as string;
                object objectToSerializePropValue = null;

                var propValue = objectType.GetProperty("Value");
                // Alternatively, get reference from Id on nested Value
                if (string.IsNullOrEmpty(valRef) && propValue != null)
                {
                    var valValue = propValue.GetValue(objectToSerialize);
                    if (valValue != null)
                    {
                        var valType = propValue.PropertyType;
                        var idProp = valType.GetProperties().FirstOrDefault(p => p.Name.ToLower() == "_id" || ((p.GetCustomAttributes(typeof(JsonPropertyAttribute), true).FirstOrDefault() as JsonPropertyAttribute)?.PropertyName?.Equals("_id")).GetValueOrDefault());
                        if (idProp != null)
                        {
                            valRef = idProp.GetValue(valValue) as string;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(valRef) && propValue != null)
                {
                    objectToSerializePropValue = propValue.GetValue(objectToSerialize);
                }

                // Get _key property (required for arrays in sanity editor)
                var keyProp = objectType.GetProperties().FirstOrDefault(p => p.Name.ToLower() == "_key" || ((p.GetCustomAttributes(typeof(JsonPropertyAttribute), true).FirstOrDefault() as JsonPropertyAttribute)?.PropertyName?.Equals("_key")).GetValueOrDefault());
                var weakProp = objectType.GetProperties().FirstOrDefault(p => p.Name.ToLower() == "_weak" || ((p.GetCustomAttributes(typeof(JsonPropertyAttribute), true).FirstOrDefault() as JsonPropertyAttribute)?.PropertyName?.Equals("_weak")).GetValueOrDefault());
                var valKey = keyProp?.GetValue(objectToSerialize) as string ?? Guid.NewGuid().ToString();
                var valWeak = weakProp?.GetValue(objectToSerialize) as bool? ?? null;


                if (!string.IsNullOrEmpty(valRef))
                {
                    serializer.Serialize(writer, new { _ref = valRef, _type = "reference", _key = valKey, _weak = valWeak, value = objectToSerializePropValue });
                    return;
                }
            }
            serializer.Serialize(writer, null);
        }
    }
}