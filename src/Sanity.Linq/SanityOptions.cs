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


namespace Sanity.Linq
{
    public class SanityOptions
    {
        public string ProjectId { get; set; }

        public string Dataset { get; set; }

        public string Token { get; set; }

        public bool UseCdn { get; set; }

        public string ApiVersion { get; set; } = "v1";
    }
}