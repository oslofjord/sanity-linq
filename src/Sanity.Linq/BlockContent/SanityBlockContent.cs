using Sanity.Linq.BlockContent.Serializers;
using Sanity.Linq.CommonTypes.BlockTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sanity.Linq.BlockContent
{
    public class SanityBlockContent
    {

        public List<ISanityBlockSerializer> seriliazers; //add customs serializers here
         

        public SanityBlockContent()
        {
            //default serializers
            //seriliazers.Add(new ImageSerializer());
            //seriliazers.Add(new BlockSerialize());
        }

        public delegate void convertBlock(SanityBlock block);

        public string ConvertToHtml(SanityBlock[] blocks)
        {
            var html = "";

            foreach (var block in blocks)
            {
                var serializer = seriliazers.FirstOrDefault(s => s.CanConvert(block.SanityType));
                if (serializer != null)
                {
                    //html += serializer.Serialize(block);
                }
            }

            //if (sanityBlock.Children.Length > 0 )
            //{
                
            //    sanityBlock.SanityType

            //    foreach (var item in sanityBlock.Children)
            //    {
            //        //get type
            //        item.

            //        //read children if any
            //        //add to html
            //    }
            //    return html;
            //}
            return "";
        }

        public string ConvertBlockBlock(SanityBlock block)
        {

            return "heey";
        }

        public string ConvertBlock(SanityBlock block)
        {

            return "heey";
        }







    }
}
