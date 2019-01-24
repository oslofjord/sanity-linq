using Sanity.Linq.CommonTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sanity.Linq.Demo.Model
{
    public class Post : SanityDocument
    {
        public Post()
        {
        }

        public string Title { get; set; }

        public SanitySlug Slug { get; set; }

        public SanityReference<Author> Author { get; set; }

        public CommonTypes.SanityImage MainImage { get; set; }

        public List<SanityReference<Category>> Categories { get; set; }

        public DateTimeOffset? PublishedAt { get; set; }

        public object Body { get; set; }
    }
}
