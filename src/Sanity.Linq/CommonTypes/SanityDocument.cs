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
using Sanity.Linq.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sanity.Linq.CommonTypes
{
    public class SanityDocument
    {
        public SanityDocument()
        {
            SanityType = GetType().GetSanityTypeName();
        }

        [JsonProperty("_id")]
        public string Id { get; set; }

        [JsonProperty("_type")]
        public string SanityType { get; set; }

        [JsonProperty("_rev")]
        public string SanityRevision { get; set; }

        [JsonProperty("_key")]
        public string SanityKey { get; set; }

        [JsonProperty("_createdAt")]
        public DateTimeOffset? SanityCreatedAt { get; set; }

        [JsonProperty("_updatedAt")]
        public DateTimeOffset? SanityUpdatedAt { get; set; }
    }
}
