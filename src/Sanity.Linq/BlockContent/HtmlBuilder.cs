using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sanity.Linq.BlockContent
{
    public class HtmlBuilder
    {
        SanityOptions _options;
        public Dictionary<string, Func<JToken, SanityOptions, string>> Serializers { get; } = new Dictionary<string, Func<JToken, SanityOptions, string>>();
        ThreeBuilder threeBuilder = new ThreeBuilder();

        public HtmlBuilder(SanityOptions options)
        {
            _options = options;
            InitSerializers();
        }

        public HtmlBuilder(SanityOptions options, Dictionary<string,Func<JToken, SanityOptions,string>> customSerializers)
        {
            _options = options;
            InitSerializers(customSerializers);
        }

        public string Build(object content)
        {
            string html = "";

            var blockArray = content as JArray;

            //build listitems (if any)
            blockArray = threeBuilder.Build(blockArray);

            //serialize each block with their respective serializers
            foreach (var block in blockArray)
            {
                html += Serialize(block);
            }

            return html;
        }

        private string Serialize(JToken block)
        {
            var type = (string)block["_type"];
            return Serializers[type](block, _options);
        }

        private void InitSerializers() //with default serializers
        {
            LoadDefaultSerializers();
        }

        private void InitSerializers(Dictionary<string, Func<JToken, SanityOptions, string>> customSerializers) //with default and custom serializers
        {
            LoadDefaultSerializers();
            foreach (var customSerializer in customSerializers)
            {
                Serializers[customSerializer.Key] = customSerializer.Value;
            }
        }

        public void LoadDefaultSerializers()
        {
            SanitySerializers serializers = new SanitySerializers();
            Serializers.Add("block", serializers.SerializeDefaultBlock);
            Serializers.Add("image", serializers.SerializeImage);
        }
    }
}
