using Newtonsoft.Json;
using Sanity.Linq.CommonTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sanity.Linq.Demo.Model
{
    public class Category
    {
        /// <summary>
        /// Use of JsonProperty to serialize to Sanity _id field.
        /// A alternative to inheriting SanityDocument class
        /// </summary>
        [JsonProperty("_id")]
        public string CategoryId { get; set; }

        /// <summary>
        /// Type field is also required
        /// </summary>
        [JsonProperty("_type")]
        public string DocumentType => "category";

        public string Title { get; set; }

        public string Description { get; set; }

        public List<Category> SubCategories { get; set; }

        [Include]
        public SanityImage MainImage { get; set; }
    }
}
