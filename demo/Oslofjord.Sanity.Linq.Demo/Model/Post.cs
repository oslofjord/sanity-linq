﻿using Oslofjord.Sanity.Linq.CommonTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Oslofjord.Sanity.Linq.Demo.Model
{
    public class Post : SanityDocument
    {
        public Post()
        {
        }

        public string Title { get; set; }

        public SanitySlug Slug { get; set; }

        public SanityReference<Author> Author { get; set; }

        public SanityImage MainImage { get; set; }

        public List<SanityReference<Category>> Categories { get; set; }

        public DateTimeOffset? PublishedAt { get; set; }

        public SanityBlock[] Body { get; set; }
    }
}
