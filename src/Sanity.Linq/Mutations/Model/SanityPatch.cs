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
using System.Text;

namespace Sanity.Linq.Mutations
{
    public class SanityPatch
    {
        
        public SanityPatch()
        {

        }


        public string IfRevisionId { get; set; }

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
