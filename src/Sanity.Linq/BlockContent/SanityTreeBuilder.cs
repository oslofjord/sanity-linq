using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sanity.Linq.BlockContent
{
    public class SanityTreeBuilder
    {
        public JArray Build(JArray blockArray)
        {
            // set list trees / listItem = bullet | number && level != null
            var currentListType = "";
            for (int i = 0; i < blockArray.Count; i++)
            {
                JObject item = (JObject)blockArray[i];

                if ((string)blockArray[i]["listItem"] == "bullet")
                {
                    //check if first in bullet array
                    if (currentListType == "" && !item.ContainsKey("firstItem"))
                    {
                        item.Add(new JProperty("firstItem", true));
                    }

                    currentListType = "bullet";

                    // check if last in array, also last in bullet array 
                    if (blockArray.Count == i+1)
                    {
                        if (!item.ContainsKey("lastItem"))
                        {
                            item.Add(new JProperty("lastItem", true));
                        }
                        currentListType = "";
                        break;
                    }

                    //in the middle of array but last of bullet array
                    if (currentListType == "bullet" && (string)blockArray[i + 1]["listItem"] == null || (string)blockArray[i + 1]["listItem"] == "number")
                    {
                        if (!item.ContainsKey("lastItem"))
                        {
                            item.Add(new JProperty("lastItem", true));
                        }
                        currentListType = "";
                    }
                }

                if ((string)blockArray[i]["listItem"] == "number")
                {
                    //check if first in bullet array
                    if (currentListType == "" && !item.ContainsKey("firstItem"))
                    {
                        item.Add(new JProperty("firstItem", true));
                    }

                    currentListType = "number";

                    // check if last in array, also last in bullet array 
                    if (blockArray.Count == i + 1)
                    {
                        if (!item.ContainsKey("lastItem"))
                        {
                            item.Add(new JProperty("lastItem", true));
                        }
                        currentListType = "";
                        break;
                    }

                    //in the middle of array but last of bullet array
                    if (currentListType == "number" && (string)blockArray[i + 1]["listItem"] == null || (string)blockArray[i + 1]["listItem"] == "bullet")
                    {
                        if (!item.ContainsKey("lastItem"))
                        {
                            item.Add(new JProperty("lastItem", true));
                        }
                        currentListType = "";
                    }
                }
            }

            return blockArray;
        }
    }
}
