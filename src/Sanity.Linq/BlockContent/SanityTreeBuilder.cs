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
                        if (blockArray[i + 1] != null)
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

            return blockArray;
        }
    }
}
