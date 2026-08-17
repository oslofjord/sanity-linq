using System;
using System.Collections.Generic;
using System.Text;
using Sanity.Linq.Json;
using System.Text.Json.Nodes;

namespace Sanity.Linq.BlockContent
{
    public class SanityTreeBuilder
    {
        public JsonArray Build(JsonArray blockArray)
        {
            // set list trees / listItem = bullet | number && level != null
            var currentListType = "";
            for (int i = 0; i < blockArray.Count; i++)
            {
                JsonObject item = (JsonObject)blockArray[i];

                if (ListItem(blockArray, i) == "bullet")
                {
                    //check if first in bullet array
                    if (currentListType == "" && !item.ContainsKey("firstItem"))
                    {
                        item["firstItem"] = true;
                    }

                    currentListType = "bullet";

                    // check if last in array, also last in bullet array
                    if (blockArray.Count == i+1)
                    {
                        if (!item.ContainsKey("lastItem"))
                        {
                            item["lastItem"] = true;
                        }
                        currentListType = "";
                        break;
                    }

                    //in the middle of array but last of bullet array
                    if (currentListType == "bullet" && ListItem(blockArray, i + 1) == null || ListItem(blockArray, i + 1) == "number")
                    {
                        if (!item.ContainsKey("lastItem"))
                        {
                            item["lastItem"] = true;
                        }
                        currentListType = "";
                    }
                }

                if (ListItem(blockArray, i) == "number")
                {
                    //check if first in bullet array
                    if (currentListType == "" && !item.ContainsKey("firstItem"))
                    {
                        item["firstItem"] = true;
                    }

                    currentListType = "number";

                    // check if last in array, also last in bullet array
                    if (blockArray.Count == i + 1)
                    {
                        if (!item.ContainsKey("lastItem"))
                        {
                            item["lastItem"] = true;
                        }
                        currentListType = "";
                        break;
                    }

                    //in the middle of array but last of bullet array
                    if (currentListType == "number" && ListItem(blockArray, i + 1) == null || ListItem(blockArray, i + 1) == "bullet")
                    {
                        if (!item.ContainsKey("lastItem"))
                        {
                            item["lastItem"] = true;
                        }
                        currentListType = "";
                    }
                }
            }

            return blockArray;
        }

        /// <summary>
        /// The "listItem" value of a block, or null when the block does not have one.
        /// </summary>
        private static string ListItem(JsonArray blockArray, int index)
        {
            return SanityJsonNode.GetString(blockArray[index], "listItem");
        }
    }
}
