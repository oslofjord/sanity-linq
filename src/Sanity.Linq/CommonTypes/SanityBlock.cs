using Sanity.Linq.BlockContent.BlockTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sanity.Linq.CommonTypes.BlockTypes
{
    public class SanityBlock : SanityObject
    {
        public SanityBlock() : base()
        {
        }

        public string Style { get; set; } = "normal";

        public object[] MarkDefs { get; set; } = new object[] { };

        public object[] Children { get; set; } = new object[] { };

        public SanityImage Asset { get; set; } = new SanityImage { };
    }
}
