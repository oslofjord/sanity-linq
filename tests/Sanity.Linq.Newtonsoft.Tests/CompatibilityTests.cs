using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Sanity.Linq.CommonTypes;
using Sanity.Linq.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
// Inside a namespace under Sanity.Linq.Newtonsoft, a qualified "Newtonsoft.Json..." would
// bind to this namespace instead of the library's, so the DOM type is aliased.
using JToken = global::Newtonsoft.Json.Linq.JToken;

namespace Sanity.Linq.Newtonsoft.Tests
{
    /// <summary>
    /// Covers what the compatibility package restores for applications migrating from
    /// Sanity LINQ 1.x, and the boundaries of that support.
    /// </summary>
    public class CompatibilityTests
    {
        private static SanityOptions Options => new SanityOptions
        {
            ProjectId = "testproject",
            Dataset = "testdataset",
            Token = "test-token",
            UseCdn = false,
        };

        private static SanityDataContext CompatContext() =>
            new SanityDataContext(Options, NewtonsoftCompat.CreateSerializerOptions());

        private static SanityDataContext PlainContext() => new SanityDataContext(Options);

        // -------------------------------------------------------------------------------
        // Queries: supported by the core library, no compatibility package required
        // -------------------------------------------------------------------------------

        [Fact]
        public void Groq_honours_newtonsoft_attributes_without_the_compat_package()
        {
            // The core library resolves [JsonProperty] reflectively when translating
            // expressions, so queries against 1.x models keep working out of the box.
            var query = PlainContext().DocumentSet<LegacyCategory>()
                .Where(c => c.CategoryId == "category-1")
                .GetSanityQuery();

            Assert.Contains("_id == \"category-1\"", query);
            Assert.DoesNotContain("categoryId", query);
        }

        [Fact]
        public void Groq_honours_renamed_fields_and_skips_ignored_ones()
        {
            var query = PlainContext().DocumentSet<LegacyCategory>().GetSanityQuery();

            Assert.DoesNotContain("notPersisted", query);
            Assert.DoesNotContain("renamedField", query);
        }

        [Fact]
        public void Groq_where_on_renamed_field_uses_the_attribute_name()
        {
            var query = PlainContext().DocumentSet<LegacyCategory>()
                .Where(c => c.RenamedField == "x")
                .GetSanityQuery();

            Assert.Contains("customFieldName == \"x\"", query);
        }

        // -------------------------------------------------------------------------------
        // Serialization: requires the compatibility package
        // -------------------------------------------------------------------------------

        [Fact]
        public void Without_the_compat_package_newtonsoft_attributes_are_not_applied()
        {
            // Documents why the package is needed: System.Text.Json does not know about
            // [JsonProperty], so the system fields would be sent under the wrong names.
            var json = PlainContext().DocumentSet<LegacyCategory>()
                .Create(new LegacyCategory { CategoryId = "category-1", Title = "T" })
                .Build();

            Assert.Contains("\"categoryId\":\"category-1\"", json);
            Assert.DoesNotContain("\"_id\"", json);
        }

        [Fact]
        public void With_the_compat_package_newtonsoft_attributes_are_applied()
        {
            var json = CompatContext().DocumentSet<LegacyCategory>()
                .Create(new LegacyCategory
                {
                    CategoryId = "category-1",
                    Title = "T",
                    RenamedField = "renamed",
                    NotPersisted = "should not appear",
                    Tags = new List<string> { "a" },
                })
                .Build();

            Assert.Contains("\"_id\":\"category-1\"", json);
            Assert.Contains("\"_type\":\"category\"", json);
            Assert.Contains("\"customFieldName\":\"renamed\"", json);
            Assert.Contains("\"title\":\"T\"", json);
            Assert.Contains("\"tags\":[\"a\"]", json);

            // [JsonIgnore] from Newtonsoft is honoured too.
            Assert.DoesNotContain("notPersisted", json);
            Assert.DoesNotContain("should not appear", json);
        }

        [Fact]
        public void Reference_resolves_id_from_a_newtonsoft_attributed_property()
        {
            // The reference converter has to locate the _id-mapped property on the
            // referenced document to derive _ref when only Value is set.
            var json = CompatContext().DocumentSet<LegacyPost>()
                .Create(new LegacyPost
                {
                    PostId = "post-1",
                    Title = "T",
                    Category = new SanityReference<LegacyCategory>
                    {
                        SanityKey = "key-1",
                        Value = new LegacyCategory { CategoryId = "category-1" },
                    },
                })
                .Build();

            Assert.Contains("\"_ref\":\"category-1\"", json);
            Assert.Contains("\"_type\":\"reference\"", json);
        }

        [Fact]
        public void Settings_translation_preserves_null_handling()
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Include,
            };

            var json = new SanityDataContext(Options, settings.ToSerializerOptions())
                .DocumentSet<LegacyCategory>()
                .Create(new LegacyCategory { CategoryId = "category-1" })
                .Build();

            // Include means nulls are written rather than dropped.
            Assert.Contains("\"title\":null", json);
        }

        [Fact]
        public void Settings_translation_preserves_a_custom_naming_strategy()
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() },
            };

            var json = new SanityDataContext(Options, settings.ToSerializerOptions())
                .DocumentSet<LegacyCategory>()
                .Create(new LegacyCategory { CategoryId = "category-1", InternalId = 3 })
                .Build();

            // Names come from Newtonsoft's own strategy, so they match 1.x exactly.
            Assert.Contains("\"internal_id\":3", json);

            // Explicit attribute names still win over the strategy.
            Assert.Contains("\"_id\":\"category-1\"", json);
        }

        [Fact]
        public void Custom_newtonsoft_converters_are_adapted()
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
                Converters = new List<JsonConverter> { new MoneyConverter() },
            };

            var json = new SanityDataContext(Options, settings.ToSerializerOptions())
                .DocumentSet<LegacyProduct>()
                .Create(new LegacyProduct { ProductId = "product-1", Price = new Money("NOK", 199.5m) })
                .Build();

            Assert.Contains("\"price\":\"NOK 199.5\"", json);
        }

        [Fact]
        public void Custom_newtonsoft_converters_are_adapted_for_reading()
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Converters = new List<JsonConverter> { new MoneyConverter() },
            };

            var options = settings.ToSerializerOptions();
            var product = System.Text.Json.JsonSerializer.Deserialize<LegacyProduct>(
                "{\"_id\":\"product-1\",\"price\":\"NOK 199.5\"}", options);

            Assert.Equal("NOK", product.Price.Currency);
            Assert.Equal(199.5m, product.Price.Amount);
        }

        // -------------------------------------------------------------------------------
        // Block content
        // -------------------------------------------------------------------------------

        [Fact]
        public async System.Threading.Tasks.Task JToken_block_serializers_still_work()
        {
            // Registered through the compatibility extension, so the delegate receives a
            // JToken exactly as it did in 1.x.
            var sanity = CompatContext();
            sanity.AddHtmlSerializer("myType", (JToken token, SanityOptions options) =>
                System.Threading.Tasks.Task.FromResult($"<custom>{token["title"]}</custom>"));

            var html = await "[{\"_type\":\"myType\",\"_key\":\"k1\",\"title\":\"Hello\"}]".ToHtmlAsync(sanity);

            Assert.Equal("<custom>Hello</custom>", html);
        }

        [Fact]
        public async System.Threading.Tasks.Task JToken_block_serializers_receive_build_context()
        {
            var sanity = CompatContext();
            sanity.AddHtmlSerializer("myType", (JToken token, SanityOptions options, object context) =>
                System.Threading.Tasks.Task.FromResult($"<custom ctx=\"{context}\">{token["title"]}</custom>"));

            var html = await "{\"_type\":\"myType\",\"_key\":\"k1\",\"title\":\"Hello\"}".ToHtmlAsync(sanity, "ctx-1");

            Assert.Equal("<custom ctx=\"ctx-1\">Hello</custom>", html);
        }

        // -------------------------------------------------------------------------------
        // Documented limits
        // -------------------------------------------------------------------------------

        [Fact]
        public void An_arbitrary_contract_resolver_is_rejected_rather_than_ignored()
        {
            var settings = new JsonSerializerSettings { ContractResolver = new CustomResolver() };

            var ex = Assert.Throws<NotSupportedException>(() => settings.ToSerializerOptions());
            Assert.Contains("cannot translate the contract resolver", ex.Message);
            Assert.Contains("IJsonTypeInfoResolver", ex.Message);
        }

        [Fact]
        public void TypeNameHandling_is_rejected()
        {
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects };

            var ex = Assert.Throws<NotSupportedException>(() => settings.ToSerializerOptions());
            Assert.Contains("TypeNameHandling", ex.Message);
        }

        [Fact]
        public void DateFormatString_is_rejected()
        {
            var settings = new JsonSerializerSettings { DateFormatString = "yyyy-MM-dd" };

            var ex = Assert.Throws<NotSupportedException>(() => settings.ToSerializerOptions());
            Assert.Contains("DateFormatString", ex.Message);
        }

        [Fact]
        public void Null_settings_yield_the_library_defaults()
        {
            JsonSerializerSettings settings = null;

            var options = settings.ToSerializerOptions();

            Assert.NotNull(options);
            Assert.Equal(System.Text.Json.JsonNamingPolicy.CamelCase, options.PropertyNamingPolicy);
        }

        private class CustomResolver : DefaultContractResolver
        {
            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                return base.CreateProperties(type, memberSerialization).Where(p => p.PropertyName != "title").ToList();
            }
        }
    }
}
