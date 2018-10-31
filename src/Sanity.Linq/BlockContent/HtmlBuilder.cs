using Newtonsoft.Json.Linq;
using Sanity.Linq.BlockContent.Serializers;
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

            // set list trees / listItem = bullet | number && level != null
            var currentListType = "";
            for (int i = 0; i < blockArray.Count - 1; i++)
            {
                JObject item = (JObject)blockArray[i];

                if ((string)blockArray[i]["listItem"] == "bullet")
                {
                    //check if first 
                    if (currentListType == "")
                    {
                        item.Add(new JProperty("firstItem", true));
                    }

                    //check if last of same list type
                    if (currentListType == "bullet")
                    {
                        if (blockArray[i + 1] != null)
                        {
                            if ((string)blockArray[i + 1]["listItem"] == "" || (string)blockArray[i + 1]["listItem"] == "number")
                            {
                                item.Add(new JProperty("lastItem", true));
                            }
                        }
                    }

                    //check if last in array
                    if (blockArray[i + 1] != null)
                    {
                        //check if next is in same list
                        if ((string)blockArray[i + 1]["listItem"] == "bullet")
                        {
                            //continue on same list
                            currentListType = "bullet";
                        }
                        else
                        {
                            //end list
                            if (blockArray[i]["lastItem"] == null)
                            {
                                item.Add(new JProperty("lastItem", true));
                            }
                            currentListType = "";
                        }
                    }
                    else
                    {
                        currentListType = "";
                    }

                }
                else if ((string)blockArray[i]["listItem"] == "number")
                {
                    //check if first 
                    if (currentListType == "")
                    {
                        item.Add(new JProperty("firstItem", true));
                    }

                    //check if last
                    if (currentListType == "number")
                    {
                        if(blockArray[i + 1] != null)
                        {
                            if ((string)blockArray[i + 1]["listItem"] == "" || (string)blockArray[i + 1]["listItem"] == "bullet")
                            {
                                item.Add(new JProperty("lastItem", true));
                            }
                        }
                    }
                    //check if last in array
                    if (blockArray[i + 1] != null)
                    {
                        //check if next is in same list
                        if ((string)blockArray[i + 1]["listItem"] == "number")
                        {
                            //continue on same list
                            currentListType = "number";
                        }
                        else
                        {
                            //end list
                            //item.Add(new JProperty("lastItem", true));
                            if (blockArray[i]["lastItem"] == null)
                            {
                                item.Add(new JProperty("lastItem", true));
                            }
                            currentListType = "";
                        }
                    }
                    else
                    {
                        currentListType = "";
                    }
                }
            }

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
