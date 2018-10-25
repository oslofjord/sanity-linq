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

namespace Sanity.Linq
{

    public enum SanityMutationVisibility
    {
        // "sync": the request will not return until the requested changes are visible to subsequent queries
        Sync = 0,

        // "async": the request will return immediately when the changes have been committed, but it might still be a second or so until you can see the change reflected in a query. For maximum performance, use "async" always, except when you need your next query to see the changes you made 
        Async = 1,

        // "deferred" is the fastest way to write. It bypasses the real time indexing completely, and should be used in cases where you are bulk importing/mutating a large number of documents and don't need to see that data in a query for several tens of seconds.
        Deferred = 2
    }
}
