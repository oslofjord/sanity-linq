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
    public class SanityOptions
    {
        public string ProjectId { get; set; }

        public string Dataset { get; set; }

        public string Token { get; set; }

        public bool UseCdn { get; set; }

        private string _apiVersion = "v1";
        
        /// <summary>
        /// The Sanity API version to use. Defaults to v1. Prefixes with "v" if not present as prefix.
        /// </summary>
        /// <exception cref="ArgumentNullException">If you try to set ApiVersion = null</exception>
        public string ApiVersion
        {
            get => _apiVersion;
            set
            {
                if (value == null)
                    throw new ArgumentNullException("ApiVersion cannot be set to null");
                
                _apiVersion = value.StartsWith("v") ? value : $"v{value}";
            }
        }
    }
}