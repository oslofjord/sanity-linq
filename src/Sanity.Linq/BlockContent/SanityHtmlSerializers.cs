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
        public Task<string> SerializeDefaultBlockAsync(JToken input, SanityOptions sanity, object context = null)
        {
            var text = new StringBuilder();
            var listStart = new StringBuilder();
            var listEnd = new StringBuilder();
            var listItemStart = new StringBuilder();
            var listItemEnd = new StringBuilder();

            var text2 = new StringBuilder();


            // get style
            var tag = "";
            if (input["style"]?.ToString() == "normal")
            {
                tag = "p";
            }
            else
            {
                //default to span
                tag = input["style"]?.ToString() ?? "span";
            }

            // get markdefs
            var markDefs = input["markDefs"];

            var isList = input["listItem"]?.ToString();

            if (input["listItem"]?.ToString() == "bullet")
            {
                listItemEnd.Append("</li>");
                // unordered <ul>
                for (var i = 0; i < ((int?)input["level"]).GetValueOrDefault(0) - 1; i++)
                {
                    listItemStart.Append("<ul>");
                    listItemEnd.Append("</ul>");
                }
                listItemStart.Append("<li>");

                //check if first or last in list
                if (input["firstItem"] != null)
                {
                    if ((bool)input["firstItem"] == true)
                    {
                        listStart.Append("<ul>");
                    }
                }
                if (input["lastItem"] != null)
                {
                    if ((bool)input["lastItem"] == true)
                    {
                        listEnd.Append("</ul>");
                    }
                }
            }

            if (input["listItem"]?.ToString() == "number")
            {
                listItemEnd.Append("</li>");
                // ordered <ol>
                for (var i = 0; i < ((int?)input["level"]).GetValueOrDefault(0) - 1; i++)
                {
                    listItemStart.Append("<ol>");
                    listItemEnd.Append("</ol>");
                }
                listItemStart.Append("<li>");

                //check if first or last in list
                if (((bool?)input["firstItem"]) == true)
                {
                    listStart.Append("<ol>");
                }
                if (((bool?)input["lastItem"]) == true)
                {
                    listEnd.Append("</ol>");
                }
            }

            // iterate through children and apply marks and add to text
            foreach (var child in input["children"])
            {
                var start = new StringBuilder();
                var end = new StringBuilder();

                if (child["marks"] != null && child["marks"].HasValues)
                {
                    foreach (var mark in child["marks"])
                    {
                        var sMark = mark?.ToString();
                        var markDef = markDefs?.FirstOrDefault(m => m["_key"]?.ToString() == sMark);
                        if (markDef != null)
                        {
                            if (TrySerializeMarkDef(markDef, context, ref start, ref end))
                            {
                                continue;
                            }
                            else if (markDef["_type"]?.ToString() == "link")
                            {
                                start.Append($"<a target=\"_blank\" href=\"{markDef["href"]?.ToString()}\">");
                                end.Append( "</a>");
                            }
                            else if (markDef["_type"]?.ToString() == "internalLink")
                            {
                                start.Append($"<a href=\"{markDef["href"]?.ToString()}\">");
                                end.Append("</a>");
                            }
                            else
                            {
                                // Mark not supported....
                            }
                        }
                        else
                        {
                            // Default
                            start.Append($"<{mark}>");
                            end.Append($"</{mark}>");
                        }
                    }
                }

                text.Append(start.ToString() + child["text"] + end.ToString());
            }

            var result = $"{listStart}{listItemStart}<{tag}>{text}</{tag}>{listItemEnd}{listEnd}".Replace("\n","</br>");

            return Task.FromResult(result);

            //return listStart + listItemStart + "<" + tag + ">" + text + "</" + tag + ">" + listItemEnd + listEnd;
        }

        public Task<string> SerializeImageAsync(JToken input, SanityOptions options)
        {
            var asset = input["asset"];
            var imageRef = asset?["_ref"]?.ToString();
            var assetValue = asset?["value"];
            var imageAltText = assetValue?["altText"];

            if (asset == null || imageRef == null)
            {
                return Task.FromResult("");
            }

            var parameters = new StringBuilder();

            if (input["query"] != null)
            {
                parameters.Append($"?{(string)input["query"]}");
            }

            //build url
            var imageParts = imageRef.Split('-');
            var url = new StringBuilder();
                url.Append("https://cdn.sanity.io/");
                url.Append(imageParts[0]     + "s/");            // images/
                url.Append(options.ProjectId + "/");             // projectid/
                url.Append(options.Dataset   + "/");             // dataset/
                url.Append(imageParts[1]     + "-");             // asset id-
                url.Append(imageParts[2]     + ".");             // dimensions.
                url.Append(imageParts[3]);                       // file extension
                url.Append(parameters.ToString());                          // ?crop etc..

            return Task.FromResult($"<figure><img src=\"{url}\" alt=\"{imageAltText}\"/></figure>");
        }
        public Task<string> SerializeTableAsync(JToken input, SanityOptions options)
        {
            var html = "";
            return Task.FromResult(html);
        }

        protected virtual bool TrySerializeMarkDef(JToken markDef, object context, ref StringBuilder start, ref StringBuilder end) => false;
    }
}