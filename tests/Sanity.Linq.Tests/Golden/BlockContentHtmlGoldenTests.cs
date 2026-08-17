using Sanity.Linq.BlockContent;
using Sanity.Linq.CommonTypes;
using Sanity.Linq.Extensions;
using System.Threading.Tasks;
using Xunit;

namespace Sanity.Linq.Tests.Golden
{
    /// <summary>
    /// Pins the HTML produced from block content.
    ///
    /// Exact string comparison: this is rendered markup, so ordering and whitespace are
    /// the contract. Covers the JSON-string input path, the strongly-typed input path and
    /// the single-block path, plus SanityTreeBuilder's list nesting.
    /// </summary>
    public class BlockContentHtmlGoldenTests
    {
        [Fact]
        public async Task From_json_array_covers_marks_markdefs_lists_and_images()
        {
            var sanity = GoldenFixtures.CreateContext();

            Assert.Equal(
                "<p><strong>Bold</strong>" +
                "<a target=\"_blank\" href=\"https://example.com\">External</a>" +
                "<a href=\"/internal\">Internal</a>" +
                "Plain</p>" +
                "<h2>A heading</h2>" +
                "<p>Line one<br />Line two</p>" +
                "<ul><li><p>Bullet one</p></li>" +
                "<ul><li><p>Bullet two nested</p></li></ul></ul>" +
                "<ol><li><p>Number one</p></li></ol>" +
                "<figure><img src=\"https://cdn.sanity.io/images/testproject/testdataset/abc123-800x600.png\"/></figure>",
                await GoldenFixtures.BlockContentJson.ToHtmlAsync(sanity));
        }

        [Fact]
        public async Task From_json_single_block_with_image_query()
        {
            var sanity = GoldenFixtures.CreateContext();

            Assert.Equal(
                "<figure><img src=\"https://cdn.sanity.io/images/testproject/testdataset/abc123-800x600.png?w=100&h=100\"/></figure>",
                await GoldenFixtures.SingleImageBlockJson.ToHtmlAsync(sanity));
        }

        [Fact]
        public async Task From_strongly_typed_objects()
        {
            // Typed input is serialized to JSON first, then rendered - so this path also
            // depends on the serializer producing camelCased Sanity field names.
            var sanity = GoldenFixtures.CreateContext();

            Assert.Equal(
                "<p><strong>A bold start!</strong> and plain text.</p>" +
                "<ul><li><p>First bullet</p></li>" +
                "<ul><li><p>Nested bullet</p></li></ul></ul>" +
                "<ol><li><p>First numbered</p></li></ol>",
                await GoldenFixtures.BlockContentObjects().ToHtmlAsync(sanity));
        }

        [Fact]
        public async Task From_strongly_typed_single_image()
        {
            var sanity = GoldenFixtures.CreateContext();

            Assert.Equal(
                "<figure><img src=\"https://cdn.sanity.io/images/testproject/testdataset/abc123-800x600.png\"/></figure>",
                await new SanityImage
                {
                    SanityKey = GoldenFixtures.Key1,
                    Asset = new SanityReference<SanityImageAsset> { Ref = "image-abc123-800x600-png", SanityKey = GoldenFixtures.Key2 },
                }.ToHtmlAsync(sanity));
        }

        [Fact]
        public async Task Newlines_become_br_tags()
        {
            // Regression cover for #75.
            var sanity = GoldenFixtures.CreateContext();
            var json = @"[{""_type"":""block"",""_key"":""b1"",""style"":""normal"",
                          ""children"":[{""_type"":""span"",""_key"":""s1"",""text"":""a\nb\nc""}]}]";

            Assert.Equal("<p>a<br />b<br />c</p>", await json.ToHtmlAsync(sanity));
        }

        [Fact]
        public async Task Unknown_type_throws_unless_ignored()
        {
            var json = @"[{""_type"":""mysteryType"",""_key"":""x1""}]";

            var strict = GoldenFixtures.CreateContext();
            await Assert.ThrowsAsync<SanityBlockContentException>(() => json.ToHtmlAsync(strict));

            var lenient = new SanityDataContext(
                GoldenFixtures.Options,
                htmlBuilderOptions: new SanityHtmlBuilderOptions { IgnoreAllUnknownTypes = true });
            Assert.Equal("", await json.ToHtmlAsync(lenient));
        }

        [Fact]
        public async Task Block_without_type_throws()
        {
            var sanity = GoldenFixtures.CreateContext();
            var json = @"[{""_key"":""x1"",""children"":[]}]";

            await Assert.ThrowsAsync<SanityBlockContentException>(() => json.ToHtmlAsync(sanity));
        }

        [Fact]
        public async Task Custom_serializer_overrides_builtin_type()
        {
            // The block-content extension point. This is the API that changes shape in 2.0
            // (JToken -> JsonNode), so pinning the behaviour matters even though the
            // signature moves.
            var sanity = GoldenFixtures.CreateContext();
            sanity.HtmlBuilder.AddSerializer("image", (node, options) =>
                Task.FromResult($"<custom>{node["asset"]?["_ref"]}</custom>"));

            Assert.Equal(
                "<custom>image-abc123-800x600-png</custom>",
                await GoldenFixtures.SingleImageBlockJson.ToHtmlAsync(sanity));
        }

        [Fact]
        public async Task Custom_serializer_receives_build_context()
        {
            var sanity = GoldenFixtures.CreateContext();
            sanity.HtmlBuilder.AddSerializer("image", (node, options, context) =>
                Task.FromResult($"<custom ctx=\"{context}\">{node["_key"]}</custom>"));

            Assert.Equal(
                "<custom ctx=\"ctx-1\">i1</custom>",
                await GoldenFixtures.SingleImageBlockJson.ToHtmlAsync(sanity, "ctx-1"));
        }
    }
}
