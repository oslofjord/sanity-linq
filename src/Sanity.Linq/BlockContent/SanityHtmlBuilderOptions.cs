using System;
using System.Collections.Generic;
using System.Text;

namespace Sanity.Linq.BlockContent
{
    public class SanityHtmlBuilderOptions
    {
        public bool IgnoreAllUnknownTypes { get; set; } = false;
        public string[] IgnoreTypes { get; set; }
    }
}
