// Copywrite 2021 Oslofjord Convention Center AS

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
using Sanity.Linq.CommonTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sanity.Linq.CommonTypes
{
    public class SanityFile : SanityObject
    {
        public SanityFile() : base()
        {
            SanityType = "file";
        }

        public SanityReference<SanityFileAsset> Asset { get; set; }
    }
}
