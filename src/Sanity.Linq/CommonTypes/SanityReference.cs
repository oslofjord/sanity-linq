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
using System;
using System.Collections.Generic;
using System.Text;

namespace Sanity.Linq.CommonTypes
{
    public class SanityReference<T> : SanityObject where T : class
    {
        public SanityReference()
        {
            SanityKey = Guid.NewGuid().ToString();
        }

        [JsonProperty("_ref")]
        public string Ref { get; set; }

        [JsonProperty("_weak")]
        public bool? Weak { get; set; }

        [JsonIgnore]
        public T Value { get; set; }

    }
}
