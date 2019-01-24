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

using Sanity.Linq.CommonTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sanity.Linq.Extensions
{
    public static class SanityExtensions
    {
        /// <summary>
        /// Used to retreived sanity type name from a class name by convention.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetSanityTypeName(this Type type)
        {
            switch (type.Name)
            {
                case nameof(SanityImageAsset):
                    {
                        return "sanity.imageAsset";
                    }
                case nameof(SanityFileAsset):
                    {
                        return "sanity.fileAsset";
                    }
                default:
                    {
                        // Remove Sanity from class name
                        var name = type.Name.Replace("Sanity", "");

                        //Make first letter lowercase (i.e. camelCase)
                        return name.ToCamelCase();
                    }
            }
            
        }
    }
}
