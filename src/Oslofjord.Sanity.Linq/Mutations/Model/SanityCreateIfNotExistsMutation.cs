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

using Oslofjord.Sanity.Linq.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Oslofjord.Sanity.Linq.Mutations
{
    public class SanityCreateIfNotExistsMutation : SanityMutation
    {
        public SanityCreateIfNotExistsMutation(object document)
        {            
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (!document.HasIdProperty()) throw new ArgumentException("Document must have an Id field which is represented as '_id' when serialized to JSON.", nameof(document));
            if (!document.HasDocumentTypeProperty()) throw new ArgumentException("Document must have an Id field which is represented as '_id' when serialized to JSON.", nameof(document));

            CreateIfNotExists = document;
        }

        public object CreateIfNotExists { get; set; }
    }
}
