// Copywrite 2018 Oslofjord Operations AS

// This file is part of Sanity LINQ (https://github.com/oslofjord/sanity-linq).

//  Sanity LINQ is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.

//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//  GNU General Public License for more details.

//  You should have received a copy of the GNU General Public License
//  along with this program.If not, see<https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oslofjord.Sanity.Linq.CommonTypes;

namespace Oslofjord.Sanity.Linq
{
    public class SanityReferenceTypeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(SanityReference<>);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
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
                    objectType.GetProperty(nameof(SanityReference<object>.Value)).SetValue(res, ((JObject)obj).ToObject(elemType));
                    return res;
                }
            }
            // Unable to deserialize
            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value != null)
            {
                var type = value.GetType();

                //Get reference from object
                var valRef = type.GetProperty("Ref").GetValue(value) as string;

                // Alternatively, get reference from Id on nested Value
                if (string.IsNullOrEmpty(valRef))
                {
                    var propValue = type.GetProperty("Value");
                    var valValue = propValue.GetValue(value);
                    if (propValue != null)
                    {
                        var valType = propValue.PropertyType;
                        var idProp = valType.GetProperties().FirstOrDefault(p => p.Name.ToLower() == "_id" || ((p.GetCustomAttributes(typeof(JsonPropertyAttribute), true).FirstOrDefault() as JsonPropertyAttribute)?.PropertyName?.Equals("_id")).GetValueOrDefault());
                        if (idProp != null)
                        {
                            valRef = idProp.GetValue(valValue) as string;
                        }
                    }
                }

                // Get _key property (required for arrays in sanity editor)
                var keyProp = type.GetProperties().FirstOrDefault(p => p.Name.ToLower() == "_key" || ((p.GetCustomAttributes(typeof(JsonPropertyAttribute), true).FirstOrDefault() as JsonPropertyAttribute)?.PropertyName?.Equals("_key")).GetValueOrDefault());
                string valKey = "";
                if (keyProp != null)
                {
                    valKey = keyProp.GetValue(value) as string;
                }
                else
                {
                    // Generate random key if _key not found
                    valKey = Guid.NewGuid().ToString();
                }

                if (!string.IsNullOrEmpty(valRef))
                {
                    serializer.Serialize(writer, new { _ref = valRef, _type = "reference", _key = valKey });
                    return;
                }
            }
            serializer.Serialize(writer, null);
        }
    }
}
