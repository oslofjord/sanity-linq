using Sanity.Linq.CommonTypes;
using Sanity.Linq.Demo.Model;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Sanity.Linq.Tests.Golden
{
    /// <summary>
    /// Offline fixtures for the golden (oracle) suite.
    ///
    /// These tests pin the library's observable JSON / GROQ / HTML output so that the
    /// Newtonsoft.Json -> System.Text.Json migration can be verified byte-for-byte.
    /// They must never require Sanity credentials or network access.
    ///
    /// Everything here is deterministic on purpose: SanityObject and SanityReference&lt;T&gt;
    /// assign a random Guid to _key in their constructors, so every fixture sets SanityKey
    /// explicitly.
    /// </summary>
    public static class GoldenFixtures
    {
        // Fixed values so goldens are stable across runs.
        public const string Key1 = "11111111-1111-1111-1111-111111111111";
        public const string Key2 = "22222222-2222-2222-2222-222222222222";
        public const string Key3 = "33333333-3333-3333-3333-333333333333";

        public static readonly DateTimeOffset PublishedAt =
            new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.FromHours(2));

        /// <summary>
        /// Dummy options: enough to construct a client/context without touching the network.
        /// </summary>
        public static SanityOptions Options => new SanityOptions
        {
            ProjectId = "testproject",
            Dataset = "testdataset",
            Token = "test-token",
            UseCdn = false,
        };

        public static SanityDataContext CreateContext() => new SanityDataContext(Options);

        /// <summary>
        /// A model that maps to Sanity system fields via attributes rather than by
        /// inheriting SanityDocument. This is the shape that exercises attribute-driven
        /// property naming.
        /// </summary>
        public static Category Category() => new Category
        {
            CategoryId = "category-1",
            Title = "Conventions",
            Description = "Some of world's greatest conventions!",
            Tags = new[] { "One", "Two" },
            Numbers = new[] { 1, 2, 3 },
        };

        public static Author Author() => new Author
        {
            Id = "author-1",
            Name = "Joe Bloggs",
            Slug = new SanitySlug("joe") { SanityKey = Key1 },
        };

        /// <summary>
        /// A document with a strong reference, a weak reference, an image and block content.
        /// Exercises SanityReferenceTypeConverter in both directions.
        /// </summary>
        public static Post Post() => new Post
        {
            Id = "post-1",
            Title = "Welcome to Oslofjord Convention Center!",
            PublishedAt = PublishedAt,
            Slug = new SanitySlug("welcome") { SanityKey = Key1 },
            Author = new SanityReference<Author> { Ref = "author-1", SanityKey = Key1 },
            Categories = new List<SanityReference<Category>>
            {
                new SanityReference<Category> { Ref = "category-1", SanityKey = Key2 },
                new SanityReference<Category> { Ref = "category-2", SanityKey = Key3, Weak = true },
            },
            MainImage = new SanityImage
            {
                SanityKey = Key1,
                Asset = new SanityReference<SanityImageAsset>
                {
                    Ref = "image-abc123-800x600-png",
                    SanityKey = Key2,
                },
            },
            Body = BlockContentObjects(),
        };

        /// <summary>
        /// Block content as strongly-typed objects (the ToHtml path for typed input).
        /// </summary>
        public static object[] BlockContentObjects() => new object[]
        {
            new SanityBlock
            {
                SanityKey = Key1,
                Children = new[]
                {
                    new SanitySpan { SanityKey = Key1, Text = "A bold start!", Marks = new[] { "strong" } },
                    new SanitySpan { SanityKey = Key2, Text = " and plain text." },
                },
            },
            new SanityBlock
            {
                SanityKey = Key2,
                ListItem = "bullet",
                Level = 1,
                Children = new[]
                {
                    new SanitySpan { SanityKey = Key1, Text = "First bullet" },
                },
            },
            new SanityBlock
            {
                SanityKey = Key3,
                ListItem = "bullet",
                Level = 2,
                Children = new[]
                {
                    new SanitySpan { SanityKey = Key1, Text = "Nested bullet" },
                },
            },
            new SanityBlock
            {
                SanityKey = Key1,
                ListItem = "number",
                Level = 1,
                Children = new[]
                {
                    new SanitySpan { SanityKey = Key2, Text = "First numbered" },
                },
            },
        };

        /// <summary>
        /// Raw block content JSON, as Sanity itself returns it. Covers marks, markDefs
        /// (link / internalLink), lists, embedded images and newline-to-br conversion.
        /// </summary>
        public const string BlockContentJson = @"[
          {
            ""_type"": ""block"",
            ""_key"": ""b1"",
            ""style"": ""normal"",
            ""markDefs"": [
              { ""_key"": ""m1"", ""_type"": ""link"", ""href"": ""https://example.com"" },
              { ""_key"": ""m2"", ""_type"": ""internalLink"", ""href"": ""/internal"" }
            ],
            ""children"": [
              { ""_type"": ""span"", ""_key"": ""s1"", ""text"": ""Bold"", ""marks"": [""strong""] },
              { ""_type"": ""span"", ""_key"": ""s2"", ""text"": ""External"", ""marks"": [""m1""] },
              { ""_type"": ""span"", ""_key"": ""s3"", ""text"": ""Internal"", ""marks"": [""m2""] },
              { ""_type"": ""span"", ""_key"": ""s4"", ""text"": ""Plain"", ""marks"": [] }
            ]
          },
          {
            ""_type"": ""block"",
            ""_key"": ""b2"",
            ""style"": ""h2"",
            ""children"": [ { ""_type"": ""span"", ""_key"": ""s1"", ""text"": ""A heading"" } ]
          },
          {
            ""_type"": ""block"",
            ""_key"": ""b3"",
            ""style"": ""normal"",
            ""children"": [ { ""_type"": ""span"", ""_key"": ""s1"", ""text"": ""Line one\nLine two"" } ]
          },
          {
            ""_type"": ""block"",
            ""_key"": ""b4"",
            ""style"": ""normal"",
            ""listItem"": ""bullet"",
            ""level"": 1,
            ""children"": [ { ""_type"": ""span"", ""_key"": ""s1"", ""text"": ""Bullet one"" } ]
          },
          {
            ""_type"": ""block"",
            ""_key"": ""b5"",
            ""style"": ""normal"",
            ""listItem"": ""bullet"",
            ""level"": 2,
            ""children"": [ { ""_type"": ""span"", ""_key"": ""s1"", ""text"": ""Bullet two nested"" } ]
          },
          {
            ""_type"": ""block"",
            ""_key"": ""b6"",
            ""style"": ""normal"",
            ""listItem"": ""number"",
            ""level"": 1,
            ""children"": [ { ""_type"": ""span"", ""_key"": ""s1"", ""text"": ""Number one"" } ]
          },
          {
            ""_type"": ""image"",
            ""_key"": ""i1"",
            ""asset"": { ""_type"": ""reference"", ""_ref"": ""image-abc123-800x600-png"" }
          }
        ]";

        /// <summary>
        /// A single (non-array) block, to cover the single-block ToHtml path.
        /// </summary>
        public const string SingleImageBlockJson = @"{
          ""_type"": ""image"",
          ""_key"": ""i1"",
          ""asset"": { ""_type"": ""reference"", ""_ref"": ""image-abc123-800x600-png"" },
          ""query"": ""w=100&h=100""
        }";

        /// <summary>
        /// A Sanity query response containing a post with a dereferenced author,
        /// a strong reference, a weak reference and system fields.
        /// Used to pin deserialization behaviour.
        /// </summary>
        public const string PostQueryResponseJson = @"{
          ""ms"": 12,
          ""query"": ""*[_type=='post']"",
          ""result"": [
            {
              ""_id"": ""post-1"",
              ""_type"": ""post"",
              ""_rev"": ""rev-1"",
              ""_createdAt"": ""2024-01-15T10:30:00Z"",
              ""_updatedAt"": ""2024-01-16T11:00:00Z"",
              ""title"": ""Welcome"",
              ""publishedAt"": ""2024-01-15T10:30:00+02:00"",
              ""slug"": { ""_type"": ""slug"", ""current"": ""welcome"" },
              ""author"": { ""_type"": ""reference"", ""_ref"": ""author-1"", ""_key"": ""k1"" },
              ""dereferencedAuthor"": {
                ""_id"": ""author-1"",
                ""_type"": ""author"",
                ""name"": ""Joe Bloggs""
              },
              ""categories"": [
                { ""_type"": ""reference"", ""_ref"": ""category-1"", ""_key"": ""k2"" },
                { ""_type"": ""reference"", ""_ref"": ""category-2"", ""_key"": ""k3"", ""_weak"": true }
              ],
              ""mainImage"": {
                ""_type"": ""image"",
                ""asset"": { ""_type"": ""reference"", ""_ref"": ""image-abc123-800x600-png"" }
              }
            }
          ]
        }";

        /// <summary>
        /// A response where a reference position instead contains the full (dereferenced)
        /// document. SanityReferenceTypeConverter has to detect the missing _ref and
        /// populate both Ref (from _id) and Value.
        /// </summary>
        public const string DereferencedReferenceResponseJson = @"{
          ""ms"": 5,
          ""query"": ""*[_type=='post']"",
          ""result"": [
            {
              ""_id"": ""post-1"",
              ""_type"": ""post"",
              ""title"": ""Welcome"",
              ""author"": {
                ""_id"": ""author-1"",
                ""_type"": ""author"",
                ""name"": ""Joe Bloggs"",
                ""slug"": { ""_type"": ""slug"", ""current"": ""joe"" }
              }
            }
          ]
        }";

        /// <summary>
        /// Same as <see cref="DereferencedReferenceResponseJson"/> but the dereferenced
        /// object also carries _key and _weak (which Sanity emits for objects inside
        /// arrays).
        ///
        /// NOTE: on 1.8.0 this THROWS - SanityReferenceTypeConverter assigns the raw
        /// JToken from _key/_weak straight onto the string/bool? properties. Fixed in 2.0.
        /// </summary>
        public const string DereferencedReferenceWithKeyResponseJson = @"{
          ""ms"": 5,
          ""query"": ""*[_type=='post']"",
          ""result"": [
            {
              ""_id"": ""post-1"",
              ""_type"": ""post"",
              ""title"": ""Welcome"",
              ""author"": {
                ""_id"": ""author-1"",
                ""_type"": ""author"",
                ""_key"": ""k1"",
                ""_weak"": true,
                ""name"": ""Joe Bloggs"",
                ""slug"": { ""_type"": ""slug"", ""current"": ""joe"" }
              }
            }
          ]
        }";

        /// <summary>
        /// Builds an HTTP 200 response so the client's real deserialization path can be
        /// exercised without a network call.
        /// </summary>
        public static HttpResponseMessage OkResponse(string json) =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
    }

    /// <summary>
    /// Exposes SanityClient's protected response handler so deserialization can be
    /// golden-tested offline, through the same code path production requests use.
    /// </summary>
    public class TestableSanityClient : SanityClient
    {
        public TestableSanityClient() : base(GoldenFixtures.Options) { }

        public Task<TResponse> DeserializeResponseAsync<TResponse>(HttpResponseMessage response) =>
            HandleHttpResponseAsync<TResponse>(response);

        public Task<TResponse> DeserializeAsync<TResponse>(string json) =>
            HandleHttpResponseAsync<TResponse>(GoldenFixtures.OkResponse(json));
    }
}
