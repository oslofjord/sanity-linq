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
using System.Text;

namespace Sanity.Linq.Mutations
{
    public class SanityPatch
    {
        
        public SanityPatch()
        {

        }


        public string IfRevisionID { get; set; }

        public object Set { get; set; }

        public object Merge { get; set; }

        public object SetIfMissing { get; set; }

        public object Unset { get; set; }

        public object Inc { get; set; }

        public object Dec { get; set; }

        public object Insert { get; set; }

        public object DiffMatchPatch { get; set; }
    }
    
}
