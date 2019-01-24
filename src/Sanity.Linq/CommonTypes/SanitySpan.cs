using System;
using System.Collections.Generic;
using System.Text;

namespace Sanity.Linq.CommonTypes
{
    public class SanitySpan : SanityObject
    {
        public SanitySpan() : base()
        {
        }

        public string Text { get; set; }

        public string[] Marks { get; set; }
    }
}
