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

namespace Sanity.Linq
{
    /// <summary>
    /// Thrown when Sanity block content cannot be converted to HTML.
    /// </summary>
    public class SanityBlockContentException : SanityException
    {
        public SanityBlockContentException()
        {
        }

        public SanityBlockContentException(string message) : base(message)
        {
        }

        public SanityBlockContentException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
