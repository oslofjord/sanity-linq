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
using System.Linq.Expressions;
using System.Text;

namespace Oslofjord.Sanity.Linq.Mutations
{
    public class SanityPatchByQuery<TDoc> : SanityPatchByQuery
    {
        public SanityPatchByQuery()
        {
        }
        public SanityPatchByQuery(Expression<Func<TDoc, bool>> query) : base()
        {
            if (query == null)
            {
                throw new ArgumentException("Query field must be set", nameof(query));
            }

            var parser = new SanityExpressionParser(query, typeof(TDoc));
            var sanityQuery = parser.BuildQuery();
            Query = sanityQuery;
        }

        public SanityPatchByQuery(Expression query) : base()
        {
            if (query == null)
            {
                throw new ArgumentException("Query field must be set", nameof(query));
            }

            var parser = new SanityExpressionParser(query, typeof(TDoc));
            var sanityQuery = parser.BuildQuery();
            Query = sanityQuery;
        }
    }

    public class SanityPatchByQuery : SanityPatch
    {
        public SanityPatchByQuery()
        {
        }

        public SanityPatchByQuery(Expression<Func<object,bool>> query) : base()
        {
            if (query == null)
            {
                throw new ArgumentException("Query field must be set", nameof(query));
            }

            var parser = new SanityExpressionParser(query, typeof(object));
            var sanityQuery = parser.BuildQuery();
            Query = sanityQuery;
        }

        public SanityPatchByQuery(string query) : base()
            {
                if (string.IsNullOrEmpty(query))
                {
                    throw new ArgumentException("Query field must be set", nameof(query));
                }

                Query = query;
            }

            public string Query { get; set; }

    }
}
