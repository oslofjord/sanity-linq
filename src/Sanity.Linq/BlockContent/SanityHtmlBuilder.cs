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
        public Dictionary<string, Func<JToken, SanityOptions, Task<string>>> Serializers { get; } = new Dictionary<string, Func<JToken, SanityOptions, Task<string>>>();
        SanityTreeBuilder treeBuilder = new SanityTreeBuilder();

        public JsonSerializerSettings SerializerSettings { get; }


        public SanityHtmlBuilder(SanityOptions options, Dictionary<string,Func<JToken,SanityOptions,Task<string>>> customSerializers = null, JsonSerializerSettings serializerSettings = null)
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
            
        }

        public virtual void AddSerializer(string type, Func<JToken, SanityOptions, Task<string>> serializeFn)
        {
            Serializers[type] = serializeFn;
        }

        public virtual Task<string> BuildAsync(object content)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (content is JArray)
            {
                return BuildAsync((JArray)content);
            }
            else if (content is JToken)
            {
                return SerializeBlockAsync((JToken)content);
            }
            else if (content is string) // JSON String
            {
                return Build((string)content);
            }
            else // Strongly typed object
            {
                var json = JsonConvert.SerializeObject(content, SerializerSettings);
                return Build(json);
            }
        }

        protected async virtual Task<string> BuildAsync(JArray content)
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
                html.Append(await SerializeBlockAsync(block).ConfigureAwait(false));
            }

            return html.ToString();
        }
        

        protected virtual Task<string> Build(string content)
        {
            var nodes = JsonConvert.DeserializeObject(content, SerializerSettings) as JToken;
            if (nodes is JArray)
            {
                // Block array (ie. block content)
                return BuildAsync((JArray)nodes);
            }
            else
            { 
                // Single block
                return SerializeBlockAsync(nodes);
            }
        }

        private Task<string> SerializeBlockAsync(JToken block)
        {
            var type = block["_type"]?.ToString();
            if (string.IsNullOrEmpty(type))
            {
                throw new Exception("Could not convert block to HTML; _type was not defined on block content.");
            }
            if (!Serializers.ContainsKey(type))
            {
                // TODO: Add options to class for ignoring/skipping types.
                throw new Exception($"No serializer for type '{type}' could be found. Consider providing a custom serializer.");
            }
            return Serializers[type](block, _options);
        }

        private void InitSerializers() //with default serializers
        {
            LoadDefaultSerializers();
        }

        private void InitSerializers(Dictionary<string, Func<JToken, SanityOptions, Task<string>>> customSerializers) //with default and custom serializers
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
            Serializers.Add("block", serializers.SerializeDefaultBlockAsync);
            Serializers.Add("image", serializers.SerializeImageAsync);
        }
    }
}
