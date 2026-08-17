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

using System.Text.Json.Serialization;
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

        [JsonPropertyName("_id")]
        public string Id { get; set; }

        [JsonPropertyName("_type")]
        public string SanityType { get; set; }

        [JsonPropertyName("_rev")]
        public string SanityRevision { get; set; }

        [JsonPropertyName("_key")]
        public string SanityKey { get; set; }

        [JsonPropertyName("_createdAt")]
        public DateTimeOffset? SanityCreatedAt { get; set; }

        [JsonPropertyName("_updatedAt")]
        public DateTimeOffset? SanityUpdatedAt { get; set; }
    }
}
