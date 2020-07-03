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

using Sanity.Linq.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sanity.Linq.Mutations
{
    public class SanityCreateOrReplaceMutation : SanityMutation
    {
        public SanityCreateOrReplaceMutation(object document)
        {            
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (!document.HasIdProperty()) throw new ArgumentException("Document must have an Id field which is represented as '_id' when serialized to JSON.", nameof(document));
            if (!document.HasDocumentTypeProperty()) throw new ArgumentException("Document must have an Id field which is represented as '_id' when serialized to JSON.", nameof(document));

            CreateOrReplace = document;
        }

        public object CreateOrReplace { get; set; }
    }
}
