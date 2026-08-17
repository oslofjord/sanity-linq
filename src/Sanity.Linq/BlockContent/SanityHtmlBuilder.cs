using Sanity.Linq.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Sanity.Linq.BlockContent
{
    public class SanityHtmlBuilder
    {
        SanityOptions _options;
        SanityHtmlBuilderOptions _htmlBuilderOptions;
        public Dictionary<string, Func<JsonNode, SanityOptions, object, Task<string>>> Serializers { get; } = new Dictionary<string, Func<JsonNode, SanityOptions, object, Task<string>>>();
        SanityTreeBuilder treeBuilder = new SanityTreeBuilder();

        public JsonSerializerOptions SerializerOptions { get; }


        public SanityHtmlBuilder(SanityOptions options,
            Dictionary<string, Func<JsonNode, SanityOptions, object, Task<string>>> customSerializers = null,
            JsonSerializerOptions serializerOptions = null,
            SanityHtmlBuilderOptions htmlBuilderOptions = null)
        {
            _options = options;
            SerializerOptions = serializerOptions ?? SanityJsonOptions.CreateDefault();
            if (customSerializers != null)
            {
                InitSerializers(customSerializers);
            }
            else
            {
                InitSerializers();
            }
            if (htmlBuilderOptions != null)
            {
                _htmlBuilderOptions = htmlBuilderOptions;
            }else
            {
                _htmlBuilderOptions = new SanityHtmlBuilderOptions();
            }

        }

        public virtual void AddSerializer(string type, Func<JsonNode, SanityOptions, Task<string>> serializeFn)
        {
            Func<JsonNode, SanityOptions, object, Task<string>> _serlializerFn = (token, options, context) => serializeFn(token, options);
            Serializers[type] = _serlializerFn;
        }

        public virtual void AddSerializer(string type, Func<JsonNode, SanityOptions, object, Task<string>> serializeFn)
        {
            Serializers[type] = serializeFn;
        }

        public virtual Task<string> BuildAsync(object content, object buildContext = null)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (content is JsonArray)
            {
                return BuildAsync((JsonArray)content, buildContext);
            }
            else if (content is JsonNode)
            {
                return SerializeBlockAsync((JsonNode)content, buildContext);
            }
            else if (content is string) // JSON String
            {
                return Build((string)content, buildContext);
            }
            else // Strongly typed object
            {
                var json = JsonSerializer.Serialize(content, SerializerOptions);
                return Build(json, buildContext);
            }
        }

        protected async virtual Task<string> BuildAsync(JsonArray content, object buildContext)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            var html = new StringBuilder();

            //build listitems (if any)
            content = treeBuilder.Build(content);

            //serialize each block with their respective serializers
            foreach (var block in content)
            {
                html.Append(await SerializeBlockAsync(block, buildContext).ConfigureAwait(false));
            }

            return html.ToString();
        }


        protected virtual Task<string> Build(string content, object buildContext)
        {
            JsonNode nodes;
            try
            {
                nodes = JsonNode.Parse(content);
            }
            catch (JsonException ex)
            {
                throw new SanitySerializationException("Could not convert block content to HTML; content was not valid JSON.", ex);
            }

            if (nodes is JsonArray)
            {
                // Block array (ie. block content)
                return BuildAsync((JsonArray)nodes, buildContext);
            }
            else
            {
                // Single block
                return SerializeBlockAsync(nodes, buildContext);
            }
        }

        private Task<string> SerializeBlockAsync(JsonNode block, object buildContext)
        {
            var type = SanityJsonNode.GetString(block, "_type");
            if (string.IsNullOrEmpty(type))
            {
                throw new SanityBlockContentException("Could not convert block to HTML; _type was not defined on block content.");
            }
            if (!Serializers.ContainsKey(type))
            {
                // TODO: Add options for ignoring/skipping specific types.
                return _htmlBuilderOptions.IgnoreAllUnknownTypes
                       ? Task.FromResult("")
                       : throw new SanityBlockContentException($"No serializer for type '{type}' could be found. Consider providing a custom serializer or setting HtmlBuilderOptions.IgnoreAllUnknownTypes.");
            }
            return Serializers[type](block, _options, buildContext);
        }

        private void InitSerializers() //with default serializers
        {
            LoadDefaultSerializers();
        }

        private void InitSerializers(Dictionary<string, Func<JsonNode, SanityOptions, object, Task<string>>> customSerializers) //with default and custom serializers
        {
            LoadDefaultSerializers();
            foreach (var customSerializer in customSerializers)
            {
                Serializers[customSerializer.Key] = customSerializer.Value;
            }
        }

        public void LoadDefaultSerializers()
        {
            var serializers = new SanityHtmlSerializers();
            AddSerializer("block", serializers.SerializeDefaultBlockAsync);
            AddSerializer("image", serializers.SerializeImageAsync);
        }
    }
}
