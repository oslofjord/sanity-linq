using Sanity.Linq.CommonTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sanity.Linq.Demo.Model
{
    /// <summary>
    /// Document type with localization support
    /// </summary>
    public class Page : SanityDocument
    {
        public SanityLocaleString Title { get; set; }

        public SanityLocale<PageOptions> Options { get; set; }
    }

    public class PageOptions
    {
        public bool ShowOnFrontPage { get; set; }

        public string Subtitle { get; set; }
    }
}
