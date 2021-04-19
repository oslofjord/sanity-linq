using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sanity.Linq.BlockContent
{
    public class SanityHtmlBuilder
    {
        SanityOptions _options;
        SanityHtmlBuilderOptions _htmlBuilderOptions;
        public Dictionary<string, Func<JToken, SanityOptions, object, Task<string>>> Serializers { get; } = new Dictionary<string, Func<JToken, SanityOptions, object, Task<string>>>();
        SanityTreeBuilder treeBuilder = new SanityTreeBuilder();

        public JsonSerializerSettings SerializerSettings { get; }


        public SanityHtmlBuilder(SanityOptions options,
            Dictionary<string,Func<JToken,SanityOptions,object,Task<string>>> customSerializers = null,
            JsonSerializerSettings serializerSettings = null,
            SanityHtmlBuilderOptions htmlBuilderOptions = null)
        {
            _options = options;
            SerializerSettings = serializerSettings ?? new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
                Converters = new List<JsonConverter> { new SanityReferenceTypeConverter() }
            };
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

        public virtual void AddSerializer(string type, Func<JToken, SanityOptions, Task<string>> serializeFn)
        {
            Func<JToken, SanityOptions, object, Task<string>> _serlializerFn = (token, options, context) => serializeFn(token, options);
            Serializers[type] = _serlializerFn;
        }

        public virtual void AddSerializer(string type, Func<JToken, SanityOptions, object, Task<string>> serializeFn)
        {
            Serializers[type] = serializeFn;
        }

        public virtual Task<string> BuildAsync(object content, object buildContext = null)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (content is JArray)
            {
                return BuildAsync((JArray)content, buildContext);
            }
            else if (content is JToken)
            {
                return SerializeBlockAsync((JToken)content, buildContext);
            }
            else if (content is string) // JSON String
            {
                return Build((string)content, buildContext);
            }
            else // Strongly typed object
            {
                var json = JsonConvert.SerializeObject(content, SerializerSettings);
                return Build(json, buildContext);
            }
        }

        protected async virtual Task<string> BuildAsync(JArray content, object buildContext)
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
            var nodes = JsonConvert.DeserializeObject(content, SerializerSettings) as JToken;
            if (nodes is JArray)
            {
                // Block array (ie. block content)
                return BuildAsync((JArray)nodes, buildContext);
            }
            else
            { 
                // Single block
                return SerializeBlockAsync(nodes, buildContext);
            }
        }

        private Task<string> SerializeBlockAsync(JToken block, object buildContext)
        {
            var type = block["_type"]?.ToString();
            if (string.IsNullOrEmpty(type))
            {
                throw new Exception("Could not convert block to HTML; _type was not defined on block content.");
            }
            if (!Serializers.ContainsKey(type))
            {
                // TODO: Add options for ignoring/skipping specific types.
                return _htmlBuilderOptions.IgnoreAllUnknownTypes 
                       ? Task.FromResult("") 
                       : throw new Exception($"No serializer for type '{type}' could be found. Consider providing a custom serializer or setting HtmlBuilderOptions.IgnoreAllUnknownTypes.");
            }
            return Serializers[type](block, _options, buildContext);
        }

        private void InitSerializers() //with default serializers
        {
            LoadDefaultSerializers();
        }

        private void InitSerializers(Dictionary<string, Func<JToken, SanityOptions, object, Task<string>>> customSerializers) //with default and custom serializers
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
