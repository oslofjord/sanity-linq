﻿// Copywrite 2018 Oslofjord Operations AS

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
using Oslofjord.Sanity.Linq.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Oslofjord.Sanity.Linq.CommonTypes
{
    public class SanityObject
    {
        public SanityObject()
        {
            SanityType = GetType().GetSanityTypeName();
            SanityKey = Guid.NewGuid().ToString();
        }

        [JsonProperty("_type")]
        public string SanityType { get; set; }

        [JsonProperty("_key")]
        public string SanityKey { get; set; }
    }
}
