using Sanity.Linq.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Sanity.Linq.BlockContent
{
    public class SanityHtmlSerializers
    {
        // Sanity Default Serializers
        public Task<string> SerializeDefaultBlockAsync(JsonNode input, SanityOptions sanity, object context = null)
        {
            var text = new StringBuilder();
            var listStart = new StringBuilder();
            var listEnd = new StringBuilder();
            var listItemStart = new StringBuilder();
            var listItemEnd = new StringBuilder();

            // get style
            var style = SanityJsonNode.GetString(input, "style");
            var tag = style == "normal" ? "p" : (style ?? "span");

            // get markdefs
            var markDefs = SanityJsonNode.GetArray(input, "markDefs");

            var listItem = SanityJsonNode.GetString(input, "listItem");
            var level = SanityJsonNode.GetInt(input, "level").GetValueOrDefault(0);

            if (listItem == "bullet")
            {
                listItemEnd.Append("</li>");
                // unordered <ul>
                for (var i = 0; i < level - 1; i++)
                {
                    listItemStart.Append("<ul>");
                    listItemEnd.Append("</ul>");
                }
                listItemStart.Append("<li>");

                //check if first or last in list
                if (SanityJsonNode.GetBool(input, "firstItem") == true)
                {
                    listStart.Append("<ul>");
                }
                if (SanityJsonNode.GetBool(input, "lastItem") == true)
                {
                    listEnd.Append("</ul>");
                }
            }

            if (listItem == "number")
            {
                listItemEnd.Append("</li>");
                // ordered <ol>
                for (var i = 0; i < level - 1; i++)
                {
                    listItemStart.Append("<ol>");
                    listItemEnd.Append("</ol>");
                }
                listItemStart.Append("<li>");

                //check if first or last in list
                if (SanityJsonNode.GetBool(input, "firstItem") == true)
                {
                    listStart.Append("<ol>");
                }
                if (SanityJsonNode.GetBool(input, "lastItem") == true)
                {
                    listEnd.Append("</ol>");
                }
            }

            // iterate through children and apply marks and add to text
            foreach (var child in SanityJsonNode.GetArray(input, "children") ?? new JsonArray())
            {
                var start = new StringBuilder();
                var end = new StringBuilder();

                var marks = SanityJsonNode.GetArray(child, "marks");
                if (marks != null && marks.Count > 0)
                {
                    foreach (var mark in marks)
                    {
                        var sMark = SanityJsonNode.ToStringValue(mark);
                        var markDef = markDefs?.FirstOrDefault(m => SanityJsonNode.GetString(m, "_key") == sMark);
                        if (markDef != null)
                        {
                            if (TrySerializeMarkDef(markDef, context, ref start, ref end))
                            {
                                continue;
                            }

                            var markDefType = SanityJsonNode.GetString(markDef, "_type");
                            if (markDefType == "link")
                            {
                                start.Append($"<a target=\"_blank\" href=\"{SanityJsonNode.GetString(markDef, "href")}\">");
                                end.Append("</a>");
                            }
                            else if (markDefType == "internalLink")
                            {
                                start.Append($"<a href=\"{SanityJsonNode.GetString(markDef, "href")}\">");
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
                            start.Append($"<{sMark}>");
                            end.Append($"</{sMark}>");
                        }
                    }
                }

                text.Append(start.ToString() + SanityJsonNode.GetString(child, "text") + end.ToString());
            }

            var result = $"{listStart}{listItemStart}<{tag}>{text}</{tag}>{listItemEnd}{listEnd}".Replace("\n", "<br />");

            return Task.FromResult(result);
        }

        public Task<string> SerializeImageAsync(JsonNode input, SanityOptions options)
        {
            var asset = SanityJsonNode.GetField(input, "asset");
            var imageRef = SanityJsonNode.GetString(asset, "_ref");

            if (asset == null || imageRef == null)
            {
                return Task.FromResult("");
            }

            var parameters = new StringBuilder();

            var query = SanityJsonNode.GetString(input, "query");
            if (query != null)
            {
                parameters.Append($"?{query}");
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

            return Task.FromResult($"<figure><img src=\"{url}\"/></figure>");
        }

        public Task<string> SerializeTableAsync(JsonNode input, SanityOptions options)
        {
            var html = "";

            return Task.FromResult(html);
        }

        protected virtual bool TrySerializeMarkDef(JsonNode markDef, object context, ref StringBuilder start, ref StringBuilder end) => false;
    }
}
