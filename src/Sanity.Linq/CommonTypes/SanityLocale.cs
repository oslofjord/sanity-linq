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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sanity.Linq.CommonTypes
{
    public class SanityLocale<T> : Dictionary<string, object>
    {
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
        

        public IReadOnlyDictionary<string, T> Translations => this.Where(kv => kv.Key != "_type").ToDictionary(kv => kv.Key, kv => {
            if (kv.Value == null) return default(T);
            if (kv.Value.GetType() == typeof(T)) return (T)kv.Value;
            if (kv.Value is JObject) return ((JObject)kv.Value).ToObject<T>();
            return default(T);
        });

        public T Get(string languageCode)
        {
            if (this.ContainsKey(languageCode))
            {
                if (this[languageCode] is JObject)
                {
                    return ((JObject)this[languageCode]).ToObject<T>();
                }
                else
                {
                    var sVal = this[languageCode]?.ToString();
                    if (sVal != null && typeof(T) == typeof(string))
                    {
                        return (T)(object)sVal;
                    }
                    return sVal != null ? (T)Convert.ChangeType(sVal, typeof(T)) : default(T);
                }
            }
            else
            {
                return default(T);
            }
        }

        public void Set(string languageCode, T value)
        {
            this[languageCode] = value;
        }

    }
}
