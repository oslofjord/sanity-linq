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
    public class SanityAsset
    {
        [JsonProperty("_id")]
        public string Id { get; set; }

        [JsonProperty("_type")]
        public string Type { get; set; }

        public string AssetId { get; set; }

        public string Extension { get; set; }

        public string Project { get; set; }

        public string Path { get; set; }

        public string Url { get; set; }

        public string Label { get; set; }

        public string OriginalFilename { get; set; }

        public long Size { get; set; }

        public SanityAssetMetadata Metadata { get; set; }

    }
}
