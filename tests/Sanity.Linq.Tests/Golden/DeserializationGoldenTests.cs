using Sanity.Linq.CommonTypes;
using Sanity.Linq.Demo.Model;
using Sanity.Linq.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Sanity.Linq.Tests.Golden
{
    /// <summary>
    /// Pins how Sanity responses map onto typed models.
    ///
    /// Runs through SanityClient's real response handler (via TestableSanityClient) so the
    /// configured deserializer, the reference converter and attribute-driven property names
    /// are all exercised, without a network call.
    /// </summary>
    public class DeserializationGoldenTests
    {
        private static TestableSanityClient Client => new TestableSanityClient();

        [Fact]
        public async Task System_fields_and_scalars()
        {
            var response = await Client.DeserializeAsync<SanityQueryResponse<List<Post>>>(GoldenFixtures.PostQueryResponseJson);
            var post = response.Result[0];

            Assert.Equal(12, response.Ms);
            Assert.Equal("post-1", post.Id);
            Assert.Equal("post", post.SanityType);
            Assert.Equal("rev-1", post.SanityRevision);
            Assert.Equal("Welcome", post.Title);

            // A trailing Z is read as UTC; an explicit offset is preserved.
            Assert.Equal("2024-01-15T10:30:00.0000000+00:00", post.SanityCreatedAt?.ToString("o"));
            Assert.Equal("2024-01-16T11:00:00.0000000+00:00", post.SanityUpdatedAt?.ToString("o"));
            Assert.Equal("2024-01-15T10:30:00.0000000+02:00", post.PublishedAt?.ToString("o"));
        }

        [Fact]
        public async Task Type_without_parameterless_constructor()
        {
            // SanitySlug only has a (string current) constructor. Both serializers can bind
            // it, but by different mechanisms, so it is worth pinning.
            var response = await Client.DeserializeAsync<SanityQueryResponse<List<Post>>>(GoldenFixtures.PostQueryResponseJson);

            Assert.Equal("welcome", response.Result[0].Slug?.Current);
        }

        [Fact]
        public async Task Strong_reference_populates_ref_but_not_value()
        {
            var response = await Client.DeserializeAsync<SanityQueryResponse<List<Post>>>(GoldenFixtures.PostQueryResponseJson);
            var author = response.Result[0].Author;

            Assert.Equal("author-1", author.Ref);
            Assert.Equal("k1", author.SanityKey);
            Assert.Equal("reference", author.SanityType);
            Assert.Null(author.Value);
        }

        [Fact]
        public async Task Weak_reference_flag_round_trips()
        {
            var response = await Client.DeserializeAsync<SanityQueryResponse<List<Post>>>(GoldenFixtures.PostQueryResponseJson);
            var categories = response.Result[0].Categories;

            Assert.Equal(2, categories.Count);
            Assert.Equal("category-1", categories[0].Ref);
            Assert.Null(categories[0].Weak);
            Assert.Equal("category-2", categories[1].Ref);
            Assert.True(categories[1].Weak);
        }

        [Fact]
        public async Task Aliased_include_target_and_nested_image_reference()
        {
            var response = await Client.DeserializeAsync<SanityQueryResponse<List<Post>>>(GoldenFixtures.PostQueryResponseJson);
            var post = response.Result[0];

            Assert.Equal("Joe Bloggs", post.DereferencedAuthor?.Name);
            Assert.Equal("image-abc123-800x600-png", post.MainImage?.Asset?.Ref);
        }

        [Fact]
        public async Task Dereferenced_object_in_reference_position_populates_ref_and_value()
        {
            // When a projection dereferences, the reference slot holds the whole document.
            // The converter has to synthesise Ref from _id and still bind Value.
            var response = await Client.DeserializeAsync<SanityQueryResponse<List<Post>>>(GoldenFixtures.DereferencedReferenceResponseJson);
            var author = response.Result[0].Author;

            Assert.Equal("author-1", author.Ref);
            Assert.Equal("reference", author.SanityType);
            Assert.Equal("Joe Bloggs", author.Value?.Name);
            Assert.Equal("author-1", author.Value?.Id);
            Assert.Equal("joe", author.Value?.Slug?.Current);
        }

        [Fact]
        public async Task Dereferenced_object_carrying_key_and_weak()
        {
            // REGRESSION: on 1.8.0 this threw, because the converter assigned the raw JSON
            // token from _key / _weak directly onto the string / bool? properties. Sanity
            // emits _key for objects inside arrays, so this shape occurs in practice.
            var response = await Client.DeserializeAsync<SanityQueryResponse<List<Post>>>(GoldenFixtures.DereferencedReferenceWithKeyResponseJson);
            var author = response.Result[0].Author;

            Assert.Equal("author-1", author.Ref);
            Assert.Equal("reference", author.SanityType);
            Assert.Equal("k1", author.SanityKey);
            Assert.True(author.Weak);
            Assert.Equal("Joe Bloggs", author.Value?.Name);
        }

        [Fact]
        public async Task Attribute_mapped_properties_are_read()
        {
            var response = await Client.DeserializeAsync<SanityQueryResponse<List<Category>>>(
                @"{""ms"":1,""query"":""q"",""result"":[{""_id"":""category-1"",""_type"":""category"",
                   ""title"":""T"",""internalId"":7,""tags"":[""a""],""numbers"":[1,2]}]}");
            var category = response.Result[0];

            Assert.Equal("category-1", category.CategoryId);
            Assert.Equal("T", category.Title);
            Assert.Equal(7, category.InternalId);
            Assert.Equal(new[] { "a" }, category.Tags);
            Assert.Equal(new[] { 1, 2 }, category.Numbers);
        }

        [Fact]
        public async Task Locale_of_scalar()
        {
            var response = await Client.DeserializeAsync<SanityQueryResponse<List<SanityLocale<string>>>>(
                @"{""ms"":1,""query"":""q"",""result"":[{""_type"":""localeString"",""en"":""Hello"",""no"":""Hei""}]}");
            var locale = response.Result[0];

            Assert.Equal("localeString", locale.Type);
            Assert.Equal("Hello", locale.Get("en"));
            Assert.Equal("Hei", locale.Get("no"));
            Assert.Equal(
                "en=Hello,no=Hei",
                string.Join(",", locale.Translations.OrderBy(kv => kv.Key).Select(kv => $"{kv.Key}={kv.Value}")));
        }

        [Fact]
        public async Task Locale_of_object()
        {
            // The dictionary values arrive as the serializer's own DOM type, and Get<T>()
            // has to convert them. This is the SanityLocale migration risk.
            var response = await Client.DeserializeAsync<SanityQueryResponse<List<SanityLocale<SanitySlug>>>>(
                @"{""ms"":1,""query"":""q"",""result"":[{""_type"":""localeSlug"",
                   ""en"":{""_type"":""slug"",""current"":""hello""}}]}");

            Assert.Equal("hello", response.Result[0].Get("en")?.Current);
        }

        [Fact]
        public async Task Locale_of_array()
        {
            var response = await Client.DeserializeAsync<SanityQueryResponse<List<SanityLocale<string[]>>>>(
                @"{""ms"":1,""query"":""q"",""result"":[{""_type"":""localeArray"",""en"":[""a"",""b""]}]}");

            Assert.Equal(new[] { "a", "b" }, response.Result[0].Get("en"));
        }

        [Fact]
        public async Task Unknown_fields_in_response_are_ignored()
        {
            var response = await Client.DeserializeAsync<SanityQueryResponse<List<Post>>>(
                @"{""ms"":1,""query"":""q"",""result"":[{""_id"":""post-1"",""_type"":""post"",
                   ""title"":""T"",""somethingTheModelDoesNotHave"":123}]}");

            Assert.Equal("post-1", response.Result[0].Id);
            Assert.Equal("T", response.Result[0].Title);
        }

        [Fact]
        public async Task Http_error_surfaces_as_SanityHttpException()
        {
            var client = Client;
            var response = new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized)
            {
                Content = new System.Net.Http.StringContent(@"{""error"":""nope""}"),
            };

            var ex = await Assert.ThrowsAsync<SanityHttpException>(
                () => client.DeserializeResponseAsync<SanityQueryResponse<List<Post>>>(response));

            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, ex.StatusCode);
            Assert.Equal(@"{""error"":""nope""}", ex.Content);
        }

        [Fact]
        public async Task Malformed_response_surfaces_as_SanitySerializationException()
        {
            await Assert.ThrowsAsync<SanitySerializationException>(
                () => Client.DeserializeAsync<SanityQueryResponse<List<Post>>>("{not json"));
        }
    }
}
