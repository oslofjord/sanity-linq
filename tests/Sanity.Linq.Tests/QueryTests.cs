using Sanity.Linq.CommonTypes;
using Sanity.Linq.CommonTypes.BlockTypes;
using Sanity.Linq.Demo.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Sanity.Linq.Tests
{

    public class QueryTests : TestBase
    {


        //[Fact]
        //public async Task CRUD_Test()
        //{

        //    var sanity = new SanityDataContext(Options);

        //    // Clear existing records in single transaction
        //    sanity.DocumentSet<Post>().Delete();
        //    sanity.DocumentSet<Author>().Delete();
        //    sanity.DocumentSet<Category>().Delete();
        //    await sanity.CommitAsync();



        //    // Create new category
        //    var categories = sanity.DocumentSet<Category>();

        //    var category1 = new Category
        //    {
        //        CategoryId = Guid.NewGuid().ToString(),
        //        Description = "Some of world's greatest conventions!",
        //        Title = "Convention"
        //    };
        //    var category2 = new Category
        //    {
        //        CategoryId = Guid.NewGuid().ToString(),
        //        Description = "Excitement, fun and relaxation...",
        //        Title = "Resort"
        //    };
        //    var category3 = new Category
        //    {
        //        CategoryId = Guid.NewGuid().ToString(),
        //        Description = "World class hotel rooms, restaurants and facilities...",
        //        Title = "Hotel"
        //    };

        //    // Chained mutations
        //    categories.Create(category1).Create(category2).Create(category3);

        //    // Create new author
        //    var author = new Author
        //    {
        //        Id = Guid.NewGuid().ToString(),
        //        Name = "Joe Bloggs",
        //        Slug = new SanitySlug("joe"),
        //    };

        //    sanity.DocumentSet<Author>().Create(author);

        //    // Create post
        //    var post = new Post
        //    {
        //        Title = "Welcome to Oslofjord Convention Center!",
        //        PublishedAt = DateTimeOffset.Now,
        //        Categories = new List<SanityReference<Category>>
        //        {
        //            new SanityReference<Category>
        //            {
        //                Ref = category1.CategoryId
        //            },
        //            new SanityReference<Category>
        //            {
        //                Ref = category2.CategoryId
        //            },
        //            new SanityReference<Category>
        //            {
        //                Ref = category3.CategoryId
        //            }
        //        },
        //        Author = new SanityReference<Author>
        //        {
        //            Value = author
        //        },
        //        Body = new[]
        //        {
        //            new SanityBlock
        //            {
        //                Children = new []
        //                {
        //                    new SanitySpan
        //                    {
        //                        Text = "A bold start!",
        //                        Marks = new[] { "strong" }
        //                    }
        //                }
        //            },
        //            new SanityBlock
        //            {
        //                Children = new []
        //                {
        //                    new SanitySpan
        //                    {
        //                        Text = "With a great article..."
        //                    }
        //                }
        //            }
        //        }
        //    };
        //    sanity.DocumentSet<Post>().Create(post);

        //    // Save all changes in one transaction
        //    var result = await sanity.CommitAsync();


        //    // LINQ Query
        //    var count = (sanity.DocumentSet<Post>().Count());
        //    var query = sanity.DocumentSet<Post>().Include(p => p.Author).Where(p => p.PublishedAt >= DateTime.Today);

        //    // Execute Query
        //    var results = await query.ToListAsync();

        //    sanity.DocumentSet<Post>().Select(p => new { p.Title, p.Author.Value.Name });

        //    Assert.True(count > 0);
        //    Assert.True(results.Count > 0);
        //    Assert.NotNull(results[0].Author?.Value);
        //    Assert.Equal("Joe Bloggs", results[0].Author.Value.Name);
        //}
    }
}
