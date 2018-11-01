using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sanity.Linq.BlockContent
{
    public class SanityHtmlSerializers
    {
        // Sanity Default Serializers
        public Task<string> SerializeDefaultBlockAsync(JToken input, SanityOptions sanity)
        {
            var text = "";
            var listStart = "";
            var listEnd = "";
            var listItemStart = "";
            var listItemEnd = "";

            var text2 = new StringBuilder();
            

            // get style
            var tag = "";
            if (input["style"]?.ToString() == "normal")
            {
                tag = "p";
            }
            else 
            {
                tag = input["style"]?.ToString() ?? "span";
            }

            // get markdefs
            var markDefs = input["markDefs"];

            var isList = input["listItem"]?.ToString();

            if (input["listItem"]?.ToString() == "bullet")
            {
                listItemEnd += "</li>";
                // unordered <ul>
                for (var i = 0; i < ((int?)input["level"]).GetValueOrDefault(0) - 1; i++)
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

            if (input["listItem"]?.ToString() == "number")
            {
                listItemEnd += "</li>";
                // ordered <ol>
                for (var i = 0; i < ((int?)input["level"]).GetValueOrDefault(0) - 1; i++)
                {
                    listItemStart += "<ol>";
                    listItemEnd += "</ol>";
                }
                listItemStart += "<li>";

                //check if first or last in list
                if (((bool?)input["firstItem"]) == true)
                {
                    listStart = "<ol>";
                }
                if (((bool?)input["lastItem"]) == true)
                {
                    listEnd = "</ol>";
                }
            }

            // iterate through children and apply marks and add to text
            foreach (var child in input["children"])
            {
                var start = "";
                var end = "";

                if(child["marks"] != null && child["marks"].HasValues)
                {
                    foreach (var mark in child["marks"])
                    {
                        var sMark = mark?.ToString();
                        var markDef = markDefs?.FirstOrDefault(m => m["_key"]?.ToString() == sMark);                       
                        if (markDef != null)
                        {
                            if (markDef["_type"]?.ToString() == "link")
                            {
                                start += @"<a href=""" + markDef["href"]?.ToString() + @""">";
                                end += "</a>";
                            }
                            else
                            {
                                // Mark not supported....
                            }
                        }
                        else
                        {
                            // Default
                            start += "<" + mark + ">";
                            end += "</" + mark + ">";
                        }
                        
                    }
                }

                text += start + child["text"] + end;
            }

            return Task.FromResult($"{listStart}{listItemStart}<{tag}>{text}</{tag}>{listItemEnd}{listEnd}");

            //return listStart + listItemStart + "<" + tag + ">" + text + "</" + tag + ">" + listItemEnd + listEnd;
        }
        public Task<string> SerializeImageAsync(JToken input, SanityOptions options)
        {
            var asset = input["asset"];
            var imageRef = asset?["_ref"]?.ToString();

            if (asset == null || imageRef == null)
            {
                return Task.FromResult("");
            }

            var parameters = "";

            if (input["query"] != null)
            {
                parameters = "?";
                parameters += (string)input["query"];
            }

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
                
            return Task.FromResult(@"<figure><img src=""" + url + @""" /></figure>");
        }
    }
}
