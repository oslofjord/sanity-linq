using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sanity.Linq.BlockContent
{
    public class SanitySerializers
    {
        public SanitySerializers()
        {

        }


        // Serializers
        public string SerializeDefaultBlock(JToken input, SanityOptions sanity)
        {
            var text = "";
            var listStart = "";
            var listEnd = "";
            var listItemStart = "";
            var listItemEnd = "";

            // get style
            var tag = "";
            if ((string)input["style"] == "normal")
            {
                tag = "p";
            }
            else 
            {
                tag = (string)input["style"];
            }

            // get markdefs
            var markDefs = input["markDefs"];

            var isList = (string)input["listItem"];

            if ((string)input["listItem"] == "bullet")
            {
                listItemEnd += "</li>";
                // unordered <ul>
                for (var i = 0; i < (int)input["level"] - 1; i++)
                {
                    listItemStart += "<ul>";
                    listItemEnd += "</ul>";
                }
                listItemStart += "<li>";

                //check if first or last in list
                if (input["firstItem"] != null)
                {
                    if ((bool)input["firstItem"] == true)
                    {
                        listStart = "<ul>";
                    }
                }
                if (input["lastItem"] != null)
                {
                    if ((bool)input["lastItem"] == true)
                    {
                        listEnd = "</ul>";
                    }
                }
            }

            if ((string)input["listItem"] == "number")
            {
                listItemEnd += "</li>";
                // ordered <ol>
                for (var i = 0; i < (int)input["level"] - 1; i++)
                {
                    listItemStart += "<ol>";
                    listItemEnd += "</ol>";
                }
                listItemStart += "<li>";

                //check if first or last in list
                if ((bool)input["firstItem"] == true)
                {
                    listStart = "<ol>";
                }
                if ((bool)input["lastItem"] == true)
                {
                    listEnd = "</ol>";
                }
            }

            // iterate through children and apply marks and add to text
            foreach (var child in input["children"])
            {
                var start = "";
                var end = "";

                if(child["marks"].HasValues)
                {
                    foreach (var mark in child["marks"])
                    {
                        foreach (var markDef in markDefs)
                        {
                            if ((string)markDef["_key"] == (string)mark)
                            {
                                start += @"<a href=""" + markDef["href"] + @""">";
                                end += "</a>";
                            }
                            else
                            {
                                start += "<" + mark + ">";
                                end += "</" + mark + ">";
                            }
                        }
                    }
                }

                text += start + child["text"] + end;
            }

            return listStart + listItemStart + "<" + tag + ">" + text + "</" + tag + ">" + listItemEnd + listEnd;
        }
        public string SerializeImage(JToken input, SanityOptions options)
        {
            var imageRef = (string)input["asset"]["_ref"];
            var parameters = "?";

            //if (input["crop"] != null && input["hotspot"] != null)
            //{
            //    parameters += crop = focalpoint & fp - x = 0.3 & fp - y = 0.2 & h = 200 & w = 300 & fit = crop
            //}

            //if (input["alt"] != null)
            //{
            //    alt = (string)input["alt"];
            //}
            //if(alt.StartsWith("?"))
            //{
            //    parameters = true;
            //}

            //if (input["crop"] != null)
            //{

            //}


            //build url
            var imageParts = imageRef.Split('-');
            var url = "https://cdn.sanity.io/";
                url += imageParts[0]     + "s/";            // images/
                url += options.ProjectId + "/";             // projectid/
                url += options.Dataset   + "/";             // dataset/
                url += imageParts[1]     + "-";             // asset id-
                url += imageParts[2]     + ".";             // dimensions.
                url += imageParts[3];                       // file extension
                url += parameters;                          // ?crop etc..
                
            return @"<figure><img src=""" + url + @""" /></figure>";
        }
    }
}
