using System;
using System.Collections.Generic;
using System.Text;

namespace Sanity.Linq.BlockContent.Serializers
{
    public abstract class SerializerBase : ISanityBlockSerializer
    {
        public string BlockType = "";

        public abstract string Serialize(string blockValue);

        public bool CanConvert(string type)
        {
            if (type == BlockType)
            {
                return true;
            }
            return false;
        }

        public abstract string Serialize(string content, string type);
    }


    public interface ISanityBlockSerializer
    {
        bool CanConvert(string id);
        string Serialize(string type);
        string Serialize(string content, string type);
    }

}
