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

namespace Sanity.Linq.DTOs
{
    public class SanityMutationResponse
    {
        public string TransactionId { get; set; }
        public SanityMutationResult[] Results { get; set; }
    }

    public class SanityMutationResponse<TDoc>
    {
        public string TransactionId { get; set; }
        public SanityMutationResult<TDoc>[] Results { get; set; }
    }
}
