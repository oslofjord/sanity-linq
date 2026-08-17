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

using Newtonsoft.Json.Linq;
using Sanity.Linq.BlockContent;
using System;
using System.Threading.Tasks;

namespace Sanity.Linq.Newtonsoft
{
    /// <summary>
    /// JToken-based block content serializers, as registered in Sanity LINQ 1.x.
    ///
    /// These live in the Sanity.Linq.Newtonsoft namespace rather than Sanity.Linq on
    /// purpose. Sharing a name and arity with the JsonNode overloads in the core library
    /// would make an implicitly-typed lambda ambiguous:
    ///
    /// <code>
    /// sanity.AddHtmlSerializer("myType", (node, options) => ...);   // which overload?
    /// </code>
    ///
    /// Requiring `using Sanity.Linq.Newtonsoft;` keeps each call site unambiguous and makes
    /// the remaining compatibility surface easy to find when it is time to drop it.
    /// </summary>
    public static class SanityNewtonsoftExtensions
    {
        /// <summary>
        /// Registers a block content serializer that receives the block as a JToken.
        /// </summary>
        public static void AddHtmlSerializer(this SanityDataContext sanity, string type, Func<JToken, SanityOptions, Task<string>> serializer)
        {
            if (sanity == null) throw new ArgumentNullException(nameof(sanity));
            if (serializer == null) throw new ArgumentNullException(nameof(serializer));

            sanity.HtmlBuilder.AddSerializer(type, (node, options) => serializer(JTokenBridge.ToJToken(node), options));
        }

        /// <summary>
        /// Registers a block content serializer that receives the block as a JToken, along
        /// with the build context.
        /// </summary>
        public static void AddHtmlSerializer(this SanityDataContext sanity, string type, Func<JToken, SanityOptions, object, Task<string>> serializer)
        {
            if (sanity == null) throw new ArgumentNullException(nameof(sanity));
            if (serializer == null) throw new ArgumentNullException(nameof(serializer));

            sanity.HtmlBuilder.AddSerializer(type, (node, options, context) => serializer(JTokenBridge.ToJToken(node), options, context));
        }

        /// <summary>
        /// Registers a JToken-based serializer directly on an html builder.
        /// </summary>
        public static void AddSerializer(this SanityHtmlBuilder builder, string type, Func<JToken, SanityOptions, Task<string>> serializer)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (serializer == null) throw new ArgumentNullException(nameof(serializer));

            builder.AddSerializer(type, (node, options) => serializer(JTokenBridge.ToJToken(node), options));
        }

        /// <summary>
        /// Registers a JToken-based serializer directly on an html builder, with build context.
        /// </summary>
        public static void AddSerializer(this SanityHtmlBuilder builder, string type, Func<JToken, SanityOptions, object, Task<string>> serializer)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (serializer == null) throw new ArgumentNullException(nameof(serializer));

            builder.AddSerializer(type, (node, options, context) => serializer(JTokenBridge.ToJToken(node), options, context));
        }
    }
}
