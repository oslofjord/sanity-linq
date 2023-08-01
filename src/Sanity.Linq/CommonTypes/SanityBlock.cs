using System;
using System.Collections.Generic;
using System.Text;

namespace Sanity.Linq.CommonTypes
{
    public class SanityBlock : SanityObject
    {
        public SanityBlock() : base()
        {
        }

        public string Style { get; set; } = "normal";

        public object[] MarkDefs { get; set; } = new object[] { };

        public object[] Children { get; set; } = new object[] { };

        [Include]
        public SanityReference<SanityImageAsset> Asset { get; set; } = new SanityReference<SanityImageAsset> { };

        public int? Level { get; set; }

        public string ListItem { get; set; }
    }
}