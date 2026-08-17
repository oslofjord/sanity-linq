using Sanity.Linq.Demo.Model;
using Sanity.Linq.Extensions;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Sanity.Linq.Tests.Golden
{
    /// <summary>
    /// Pins the exact GROQ emitted by SanityExpressionParser.
    ///
    /// GROQ is whitespace- and order-sensitive, so these are exact string comparisons.
    /// They run offline: GetSanityQuery() only walks the expression tree.
    /// </summary>
    public class GroqQueryGoldenTests
    {
        private static SanityDataContext Sanity => GoldenFixtures.CreateContext();

        // The default Post projection, repeated in most goldens below.
        private const string PostProjection =
            "{...,slug,author,\"dereferencedAuthor\":author{...,slug,favoriteCategories[]->{...,subCategories[]{...,subCategories[]{...,subCategories[]{...,subCategories[]{...,mainImage},mainImage{...,asset->,crop,hotspot}},mainImage{...,asset->,crop,hotspot}},mainImage{...,asset->,crop,hotspot}},mainImage{...,asset->,crop,hotspot}},images[]{...,asset->,crop,hotspot}},mainImage{...,asset,crop,hotspot}}";

        // The default Category projection (recursive SubCategories, capped by nesting level).
        private const string CategoryProjection =
            "{...,subCategories[]{...,subCategories[]{...,subCategories[]{...,subCategories[]{...,subCategories[]{...,subCategories[]{...,mainImage},mainImage{...,asset->,crop,hotspot}},mainImage{...,asset->,crop,hotspot}},mainImage{...,asset->,crop,hotspot}},mainImage{...,asset->,crop,hotspot}},mainImage{...,asset->,crop,hotspot}},mainImage{...,asset->,crop,hotspot}}";

        [Fact]
        public void All_documents_of_type()
        {
            Assert.Equal(
                "*[_type == \"post\"]" + PostProjection,
                Sanity.DocumentSet<Post>().GetSanityQuery());
        }

        [Fact]
        public void Where_string_equality()
        {
            Assert.Equal(
                "*[(_type == \"post\") && ((title == \"Welcome\"))]" + PostProjection,
                Sanity.DocumentSet<Post>().Where(p => p.Title == "Welcome").GetSanityQuery());
        }

        [Fact]
        public void Where_datetimeoffset_comparison()
        {
            // Pins how a DateTimeOffset constant is rendered into GROQ.
            Assert.Equal(
                "*[(_type == \"post\") && ((publishedAt >= \"2024-01-15T10:30:00.0000000+02:00\"))]" + PostProjection,
                Sanity.DocumentSet<Post>().Where(p => p.PublishedAt >= GoldenFixtures.PublishedAt).GetSanityQuery());
        }

        [Fact]
        public void Where_boolean_and()
        {
            Assert.Equal(
                "*[(_type == \"category\") && (((title == \"x\") && (internalId > 3)))]" + CategoryProjection,
                Sanity.DocumentSet<Category>().Where(c => c.Title == "x" && c.InternalId > 3).GetSanityQuery());
        }

        [Fact]
        public void Where_uses_attribute_mapped_field_name()
        {
            // Category.CategoryId is mapped to the Sanity "_id" field by attribute, not by
            // convention. This is the query-side half of the attribute-naming contract.
            Assert.Equal(
                "*[(_type == \"category\") && ((_id == \"category-1\"))]" + CategoryProjection,
                Sanity.DocumentSet<Category>().Where(c => c.CategoryId == "category-1").GetSanityQuery());
        }

        [Fact]
        public void OrderBy_descending()
        {
            Assert.Equal(
                "*[_type == \"post\"]" + PostProjection + " | order(publishedAt desc)",
                Sanity.DocumentSet<Post>().OrderByDescending(p => p.PublishedAt).GetSanityQuery());
        }

        [Fact]
        public void OrderBy_with_skip_and_take()
        {
            // KNOWN DEFECT (pre-dates the System.Text.Json migration): combining OrderBy with
            // Skip/Take repeats the ordering clause once per re-visit of the expression tree.
            // Pinned as-is so the migration is provably behaviour-preserving; fixing it is a
            // separate change that should update this golden deliberately.
            Assert.Equal(
                "*[_type == \"post\"]" + PostProjection + " | order(title asc, title asc, title asc, title asc) [5..14]",
                Sanity.DocumentSet<Post>().OrderBy(p => p.Title).Skip(5).Take(10).GetSanityQuery());
        }

        [Fact]
        public void Select_projection()
        {
            Assert.Equal(
                "*[_type == \"post\"]{title,\"id\":_id}",
                Sanity.DocumentSet<Post>().Select(p => new { p.Title, p.Id }).GetSanityQuery());
        }

        [Fact]
        public void Contains_on_local_list_becomes_in_operator()
        {
            Assert.Equal(
                "*[(_type == \"category\") && (title in [\"a\",\"b\"])]" + CategoryProjection,
                Sanity.DocumentSet<Category>().Where(c => new List<string> { "a", "b" }.Contains(c.Title)).GetSanityQuery());
        }

        [Fact]
        public void Contains_on_document_field_becomes_in_operator()
        {
            Assert.Equal(
                "*[(_type == \"category\") && (\"Two\" in tags)]" + CategoryProjection,
                Sanity.DocumentSet<Category>().Where(c => c.Tags.Contains("Two")).GetSanityQuery());
        }

        [Fact]
        public void Include_dereferences_property()
        {
            Assert.Equal(
                "*[_type == \"post\"]{...,slug,\"dereferencedAuthor\":author{...,slug,favoriteCategories[]->{...,subCategories[]{...,subCategories[]{...,subCategories[]{...,subCategories[]{...,mainImage},mainImage{...,asset->,crop,hotspot}},mainImage{...,asset->,crop,hotspot}},mainImage{...,asset->,crop,hotspot}},mainImage{...,asset->,crop,hotspot}},images[]{...,asset->,crop,hotspot}},mainImage{...,asset,crop,hotspot},author->{...,slug,favoriteCategories[]->{...,subCategories[]{...,subCategories[]{...,subCategories[]{...,subCategories[]{...,subCategories[]{...,mainImage},mainImage{...,asset->,crop,hotspot}},mainImage{...,asset->,crop,hotspot}},mainImage{...,asset->,crop,hotspot}},mainImage{...,asset->,crop,hotspot}},mainImage{...,asset->,crop,hotspot}},images[]{...,asset->,crop,hotspot}}}",
                Sanity.DocumentSet<Post>().Include(p => p.Author).GetSanityQuery());
        }

        [Fact]
        public void Include_with_source_name_alias()
        {
            Assert.Equal(
                "*[_type == \"post\"]{...,slug,author,mainImage{...,asset,crop,hotspot},\"dereferencedAuthor\":author{...,slug,favoriteCategories[]->{...,subCategories[]{...,subCategories[]{...,subCategories[]{...,subCategories[]{...,subCategories[]{...,mainImage},mainImage{...,asset->,crop,hotspot}},mainImage{...,asset->,crop,hotspot}},mainImage{...,asset->,crop,hotspot}},mainImage{...,asset->,crop,hotspot}},mainImage{...,asset->,crop,hotspot}},images[]{...,asset->,crop,hotspot}}}",
                Sanity.DocumentSet<Post>().Include(p => p.DereferencedAuthor, "author").GetSanityQuery());
        }

        [Fact]
        public void Include_nested_path()
        {
            Assert.Equal(
                "*[_type == \"post\"]{...,slug,author{...,favoriteCategories[]->{...,subCategories[]{...,subCategories[]{...,subCategories[]{...,subCategories[]{...,subCategories[]{...,subCategories[]{...,mainImage},mainImage{...,asset->,crop,hotspot}},mainImage{...,asset->,crop,hotspot}},mainImage{...,asset->,crop,hotspot}},mainImage{...,asset->,crop,hotspot}},mainImage{...,asset->,crop,hotspot}},mainImage{...,asset->,crop,hotspot}}},\"dereferencedAuthor\":author{...,slug,favoriteCategories[]->{...,subCategories[]{...,subCategories[]{...,subCategories[]{...,subCategories[]{...,mainImage},mainImage{...,asset->,crop,hotspot}},mainImage{...,asset->,crop,hotspot}},mainImage{...,asset->,crop,hotspot}},mainImage{...,asset->,crop,hotspot}},images[]{...,asset->,crop,hotspot}},mainImage{...,asset,crop,hotspot}}",
                Sanity.DocumentSet<Post>().Include(p => p.Author.Value.FavoriteCategories).GetSanityQuery());
        }

        [Fact]
        public void Include_multiple_merges_projections()
        {
            // The most demanding path: several includes merged into one projection. This is
            // the code that round-trips GROQ through a JSON DOM, so it is the sharpest test
            // of the DOM migration.
            Assert.Equal(
                "*[_type == \"post\"]{...,slug,mainImage{...,asset,crop,hotspot},author->{...,slug,favoriteCategories[]->{...,subCategories[]{...,subCategories[]{...,subCategories[]{...,subCategories[]{...,subCategories[]{...,mainImage},mainImage{...,asset->,crop,hotspot}},mainImage{...,asset->,crop,hotspot}},mainImage{...,asset->,crop,hotspot}},mainImage{...,asset->,crop,hotspot}},mainImage{...,asset->,crop,hotspot}},images[]{...,asset->,crop,hotspot}},categories[]->{...,subCategories[]{...,subCategories[]{...,subCategories[]{...,subCategories[]{...,subCategories[]{...,subCategories[]{...,mainImage},mainImage{...,asset->,crop,hotspot}},mainImage{...,asset->,crop,hotspot}},mainImage{...,asset->,crop,hotspot}},mainImage{...,asset->,crop,hotspot}},mainImage{...,asset->,crop,hotspot}},mainImage{...,asset->,crop,hotspot}},\"dereferencedAuthor\":author{...,slug,favoriteCategories[]->{...,subCategories[]{...,subCategories[]{...,subCategories[]{...,subCategories[]{...,subCategories[]{...,mainImage},mainImage{...,asset->,crop,hotspot}},mainImage{...,asset->,crop,hotspot}},mainImage{...,asset->,crop,hotspot}},mainImage{...,asset->,crop,hotspot}},mainImage{...,asset->,crop,hotspot}},images[]{...,asset->,crop,hotspot}}}",
                Sanity.DocumentSet<Post>()
                    .Include(p => p.Author)
                    .Include(p => p.DereferencedAuthor, "author")
                    .Include(p => p.Categories)
                    .Include(p => p.Author.Value.Images)
                    .GetSanityQuery());
        }

        [Fact]
        public void Default_projection_for_recursive_type()
        {
            Assert.Equal(
                "*[_type == \"category\"]" + CategoryProjection,
                Sanity.DocumentSet<Category>().GetSanityQuery());
        }
    }
}
