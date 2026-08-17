using Sanity.Linq.Demo.Model;
using Sanity.Linq.Extensions;
using System.Linq;
using Xunit;

namespace Sanity.Linq.Tests.Golden
{
    /// <summary>
    /// Regression cover for expression-tree traversal.
    ///
    /// SanityExpressionParser.Visit used to walk a method call's source argument in addition
    /// to the walk each case in TransformMethodCallExpression already performs. That visited
    /// the innermost call 2^depth times, so every ordering was appended once per visit:
    /// .OrderBy(t).Skip(5).Take(10) produced "order(t asc, t asc, t asc, t asc)".
    ///
    /// Duplicate constraints were produced too, but Build() applies Distinct() to those, so
    /// only orderings showed the symptom. These tests assert on operator chains of varying
    /// depth, which is what the duplication scaled with.
    /// </summary>
    public class OrderingTests
    {
        // A fresh context per query: Include mutates the document set's Expression in place,
        // and document sets are cached per context.
        private static SanityDocumentSet<Post> Posts => GoldenFixtures.CreateContext().DocumentSet<Post>();

        /// <summary>
        /// The ordering and slice clauses, without the projection.
        /// </summary>
        private static string ClausesOf(string groq)
        {
            var projectionStart = groq.IndexOf(']') + 1;
            var clauses = groq.Substring(projectionStart);
            var order = clauses.IndexOf(" | order(");
            return order >= 0 ? clauses.Substring(order) : "";
        }

        [Fact]
        public void OrderBy_alone_is_the_control_case()
        {
            // Depth 1 was never affected: the source is a ConstantExpression, which the old
            // duplicate walk skipped.
            Assert.Equal(" | order(title asc)", ClausesOf(Posts.OrderBy(p => p.Title).GetSanityQuery()));
        }

        [Fact]
        public void OrderBy_then_take()
        {
            Assert.Equal(" | order(title asc) [0..9]", ClausesOf(Posts.OrderBy(p => p.Title).Take(10).GetSanityQuery()));
        }

        [Fact]
        public void OrderBy_then_skip()
        {
            Assert.Equal(" | order(title asc) [5..2147483647]", ClausesOf(Posts.OrderBy(p => p.Title).Skip(5).GetSanityQuery()));
        }

        [Fact]
        public void OrderBy_then_skip_and_take()
        {
            Assert.Equal(" | order(title asc) [5..14]", ClausesOf(Posts.OrderBy(p => p.Title).Skip(5).Take(10).GetSanityQuery()));
        }

        [Fact]
        public void ThenBy_keeps_both_keys_in_order_and_adds_nothing_else()
        {
            // The previous behaviour appended a third key: "title asc, _id asc, title asc".
            Assert.Equal(
                " | order(title asc, _id asc)",
                ClausesOf(Posts.OrderBy(p => p.Title).ThenBy(p => p.Id).GetSanityQuery()));
        }

        [Fact]
        public void ThenByDescending_keeps_both_keys_in_order()
        {
            Assert.Equal(
                " | order(title desc, _id desc)",
                ClausesOf(Posts.OrderByDescending(p => p.Title).ThenByDescending(p => p.Id).GetSanityQuery()));
        }

        [Fact]
        public void ThenBy_survives_a_following_slice()
        {
            // Previously six keys.
            Assert.Equal(
                " | order(title asc, _id asc) [0..2]",
                ClausesOf(Posts.OrderBy(p => p.Title).ThenBy(p => p.Id).Take(3).GetSanityQuery()));
        }

        [Fact]
        public void Where_before_ordering_and_slicing()
        {
            var groq = Posts.Where(p => p.Title == "a").OrderBy(p => p.Title).Take(2).GetSanityQuery();

            Assert.StartsWith("*[(_type == \"post\") && ((title == \"a\"))]", groq);
            Assert.Equal(" | order(title asc) [0..1]", ClausesOf(groq));
        }

        [Fact]
        public void Chained_where_clauses_are_each_applied_once()
        {
            // Build() de-duplicates constraints, so this passed even while the traversal was
            // doubling them. Pinned to keep it that way now the traversal is single.
            Assert.StartsWith(
                "*[((_type == \"post\") && ((title == \"a\"))) && ((_id == \"b\"))]",
                Posts.Where(p => p.Title == "a").Where(p => p.Id == "b").GetSanityQuery());
        }

        [Fact]
        public void Include_still_traverses_its_source()
        {
            // Include was the one operator that relied on the removed outer traversal to reach
            // its source, so a Where underneath it has to survive.
            var groq = GoldenFixtures.CreateContext().DocumentSet<Post>()
                .Where(p => p.Title == "a")
                .Include(p => p.Author)
                .GetSanityQuery();

            Assert.StartsWith("*[(_type == \"post\") && ((title == \"a\"))]", groq);
            Assert.Contains("author->{", groq);
        }

        [Fact]
        public void Include_above_an_ordering_keeps_both()
        {
            var groq = GoldenFixtures.CreateContext().DocumentSet<Post>()
                .Include(p => p.Author)
                .OrderBy(p => p.Title)
                .GetSanityQuery();

            Assert.Contains("author->{", groq);
            Assert.Equal(" | order(title asc)", ClausesOf(groq));
        }
    }
}
