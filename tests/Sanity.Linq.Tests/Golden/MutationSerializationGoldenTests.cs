using Sanity.Linq.CommonTypes;
using Sanity.Linq.Demo.Model;
using Sanity.Linq.Extensions;
using Sanity.Linq.Mutations;
using System;
using Xunit;

namespace Sanity.Linq.Tests.Golden
{
    /// <summary>
    /// Pins the JSON the library sends on the wire for mutations.
    ///
    /// SanityMutationBuilder&lt;TDoc&gt;.Build() serializes with the context's configured
    /// serializer, so these goldens cover the whole serialization contract in one place:
    /// property naming, null omission, attribute-mapped field names, reference conversion
    /// and date formatting.
    ///
    /// Compared with JsonAssert.Equivalent: field presence, values and array order are the
    /// contract; the order of keys within an object is not, and the library should not be
    /// pinned to it.
    /// </summary>
    public class MutationSerializationGoldenTests
    {
        private static string Build<TDoc>(Func<SanityDataContext, SanityMutationBuilder<TDoc>> mutate)
        {
            var context = GoldenFixtures.CreateContext();
            return mutate(context).Build();
        }

        [Fact]
        public void Create_document_with_attribute_mapped_system_fields()
        {
            // Category maps _id / _type via attributes and has a get-only _type property.
            // Note InternalId serializes as 0 rather than being omitted: NullValueHandling
            // only drops nulls, not default value types.
            JsonAssert.Equivalent(
                "{\"mutations\":[{\"create\":{\"_id\":\"category-1\",\"_type\":\"category\",\"internalId\":0,\"title\":\"Conventions\",\"description\":\"Some of world's greatest conventions!\",\"tags\":[\"One\",\"Two\"],\"numbers\":[1,2,3]}}]}",
                Build(c => c.DocumentSet<Category>().Create(GoldenFixtures.Category())));
        }

        [Fact]
        public void Create_document_inheriting_SanityDocument()
        {
            JsonAssert.Equivalent(
                "{\"mutations\":[{\"create\":{\"name\":\"Joe Bloggs\",\"slug\":{\"current\":\"joe\",\"_type\":\"slug\",\"_key\":\"11111111-1111-1111-1111-111111111111\"},\"_id\":\"author-1\",\"_type\":\"author\"}}]}",
                Build(c => c.DocumentSet<Author>().Create(GoldenFixtures.Author())));
        }

        [Fact]
        public void Create_document_with_references_images_and_block_content()
        {
            // The full surface: strong reference, weak reference, nested image reference,
            // DateTimeOffset, and block content. Note "asset":null - SanityBlock.Asset
            // defaults to a non-null empty SanityReference, and the reference converter
            // writes an explicit null for a reference with no _ref.
            JsonAssert.Equivalent(ExpectedPostCreate, Build(c => c.DocumentSet<Post>().Create(GoldenFixtures.Post())));
        }

        [Fact]
        public void Update_emits_createOrReplace()
        {
            JsonAssert.Equivalent(
                ExpectedPostCreate.Replace("\"create\":", "\"createOrReplace\":"),
                Build(c => c.DocumentSet<Post>().Update(GoldenFixtures.Post())));
        }

        [Fact]
        public void SetValues_emits_patch_with_set()
        {
            JsonAssert.Equivalent(
                "{\"mutations\":[{\"patch\":{\"id\":\"post-1\",\"set\":" + PostBody + "}}]}",
                Build(c => c.DocumentSet<Post>().SetValues(GoldenFixtures.Post())));
        }

        [Fact]
        public void Reference_with_only_Value_set_resolves_ref_from_nested_id()
        {
            // The converter has to find the _id-mapped property on Author (declared via
            // attribute on the base class) to derive _ref. Value itself is not serialized.
            JsonAssert.Equivalent(
                "{\"mutations\":[{\"create\":{\"title\":\"Ref by value\",\"author\":{\"_ref\":\"author-1\",\"_type\":\"reference\",\"_key\":\"11111111-1111-1111-1111-111111111111\"},\"_id\":\"post-2\",\"_type\":\"post\"}}]}",
                Build(c => c.DocumentSet<Post>().Create(new Post
                {
                    Id = "post-2",
                    Title = "Ref by value",
                    Author = new SanityReference<Author> { SanityKey = GoldenFixtures.Key1, Value = GoldenFixtures.Author() },
                })));
        }

        [Fact]
        public void DeleteById()
        {
            JsonAssert.Equivalent(
                "{\"mutations\":[{\"delete\":{\"id\":\"post-1\"}}]}",
                Build(c => c.DocumentSet<Post>().DeleteById("post-1")));
        }

        [Fact]
        public void DeleteByQuery_embeds_groq_without_projection()
        {
            JsonAssert.Equivalent(
                "{\"mutations\":[{\"delete\":{\"query\":\"*[(_type == \\\"post\\\") && ((title == \\\"Welcome\\\"))]\"}}]}",
                Build(c => c.DocumentSet<Post>().DeleteByQuery(p => p.Title == "Welcome")));
        }

        [Fact]
        public void PatchById_with_set_unset_and_inc()
        {
            // Patch operations serialize in SanityPatch declaration order, not call order.
            JsonAssert.Equivalent(
                "{\"mutations\":[{\"patch\":{\"id\":\"post-1\",\"set\":{\"title\":\"New\",\"publishedAt\":\"2024-01-15T10:30:00+02:00\"},\"unset\":[\"subtitle\"],\"inc\":{\"views\":1}}}]}",
                Build(c => c.DocumentSet<Post>().PatchById("post-1", p =>
                {
                    p.Set = new { title = "New", publishedAt = GoldenFixtures.PublishedAt };
                    p.Inc = new { views = 1 };
                    p.Unset = new[] { "subtitle" };
                })));
        }

        [Fact]
        public void PatchByQuery_embeds_groq_with_projection()
        {
            // Note: unlike DeleteByQuery, the patch query carries the full projection.
            // Pre-existing asymmetry, pinned so the migration does not silently change it.
            JsonAssert.Equivalent(
                "{\"mutations\":[{\"patch\":{\"query\":\"*[(_type == \\\"post\\\") && ((title == \\\"Welcome\\\"))]{...,slug,author,\\\"dereferencedAuthor\\\":author{...,slug,favoriteCategories[]->,images[]},mainImage{...,asset,crop,hotspot}}\",\"set\":{\"title\":\"New\"}}}]}",
                Build(c => c.DocumentSet<Post>().PatchByQuery(p => p.Title == "Welcome", p => p.Set = new { title = "New" })));
        }

        // ---------------------------------------------------------------------------
        // Shared expected payloads
        // ---------------------------------------------------------------------------

        private const string PostBody =
            "{\"title\":\"Welcome to Oslofjord Convention Center!\"," +
            "\"slug\":{\"current\":\"welcome\",\"_type\":\"slug\",\"_key\":\"11111111-1111-1111-1111-111111111111\"}," +
            "\"author\":{\"_ref\":\"author-1\",\"_type\":\"reference\",\"_key\":\"11111111-1111-1111-1111-111111111111\"}," +
            "\"mainImage\":{\"asset\":{\"_ref\":\"image-abc123-800x600-png\",\"_type\":\"reference\",\"_key\":\"22222222-2222-2222-2222-222222222222\"},\"_type\":\"image\",\"_key\":\"11111111-1111-1111-1111-111111111111\"}," +
            "\"categories\":[{\"_ref\":\"category-1\",\"_type\":\"reference\",\"_key\":\"22222222-2222-2222-2222-222222222222\"},{\"_ref\":\"category-2\",\"_type\":\"reference\",\"_key\":\"33333333-3333-3333-3333-333333333333\",\"_weak\":true}]," +
            "\"publishedAt\":\"2024-01-15T10:30:00+02:00\"," +
            "\"body\":[" +
                "{\"style\":\"normal\",\"markDefs\":[],\"children\":[{\"text\":\"A bold start!\",\"marks\":[\"strong\"],\"_type\":\"span\",\"_key\":\"11111111-1111-1111-1111-111111111111\"},{\"text\":\" and plain text.\",\"_type\":\"span\",\"_key\":\"22222222-2222-2222-2222-222222222222\"}],\"asset\":null,\"_type\":\"block\",\"_key\":\"11111111-1111-1111-1111-111111111111\"}," +
                "{\"style\":\"normal\",\"markDefs\":[],\"children\":[{\"text\":\"First bullet\",\"_type\":\"span\",\"_key\":\"11111111-1111-1111-1111-111111111111\"}],\"asset\":null,\"level\":1,\"listItem\":\"bullet\",\"_type\":\"block\",\"_key\":\"22222222-2222-2222-2222-222222222222\"}," +
                "{\"style\":\"normal\",\"markDefs\":[],\"children\":[{\"text\":\"Nested bullet\",\"_type\":\"span\",\"_key\":\"11111111-1111-1111-1111-111111111111\"}],\"asset\":null,\"level\":2,\"listItem\":\"bullet\",\"_type\":\"block\",\"_key\":\"33333333-3333-3333-3333-333333333333\"}," +
                "{\"style\":\"normal\",\"markDefs\":[],\"children\":[{\"text\":\"First numbered\",\"_type\":\"span\",\"_key\":\"22222222-2222-2222-2222-222222222222\"}],\"asset\":null,\"level\":1,\"listItem\":\"number\",\"_type\":\"block\",\"_key\":\"11111111-1111-1111-1111-111111111111\"}]," +
            "\"_id\":\"post-1\",\"_type\":\"post\"}";

        private const string ExpectedPostCreate = "{\"mutations\":[{\"create\":" + PostBody + "}]}";
    }
}
