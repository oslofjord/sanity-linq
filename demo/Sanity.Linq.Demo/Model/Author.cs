using Sanity.Linq.CommonTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sanity.Linq.Demo.Model
{
    public class Author : SanityDocument
    {
        public Author() 
            : base()
        {
        }

        public string Name { get; set; }

        public SanitySlug Slug { get; set; }

        [Include]
        public SanityImage Image { get; set; }

        public object[] Bio { get; set; }
    }
}
