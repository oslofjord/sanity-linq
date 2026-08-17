using Sanity.Linq.CommonTypes;
using Sanity.Linq.Demo.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Sanity.Linq.Extensions;

namespace Sanity.Linq.Tests
{

    public class QueryTests : TestBase
    {
        [Fact]
        public async Task Contains_Test()
        {

            var sanity = new SanityDataContext(Options);
            await ClearAllDataAsync(sanity);
            var categories = sanity.DocumentSet<Category>();

            // Create categoriues
            var category1 = new Category
            {
                CategoryId = Guid.NewGuid().ToString(),
                Description = "Some of world's greatest conventions!",
                Tags = new[] { "One", "Two", "Three" },
                Numbers = new[] { 1, 2, 3 },
                Title = "Conventions",
                InternalId = 1
            };
            var category2 = new Category
            {
                CategoryId = Guid.NewGuid().ToString(),
                Description = "Some of world's greatest events!",
                Tags = new[] { "Four", "Five", "Six" },
                Numbers = new[] { 4, 5, 6 },
                Title = "Events",
                InternalId = 2
            };
            var category3 = new Category
            {
                CategoryId = Guid.NewGuid().ToString(),
                Description = "Some of world's greatest festivals!",
                Tags = new[] { "Seven", "Eight", "Nine" },
                Numbers = new[] { 7, 8, 9 },
                Title = "Festivals",
                InternalId = 3
            };
            await categories
                    .Create(category1)
                    .Create(category2)
                    .Create(category3)
                    .CommitAsync();

            // Test 1
            // *[title in ["Conventions", "Festivals"]]
            // .Where(p => ids.Contains(p.Id))

            var namesToFind = new List<string> { "Conventions", "Festivals" };
            var result1 = await categories.Where(c => namesToFind.Contains(c.Title)).ToListAsync();
            Assert.True(result1.Count == 2);

            var idsToFind = new List<int> { 1, 2 };
            var result2 = await categories.Where(c => idsToFind.Contains(c.InternalId)).ToListAsync();
            Assert.True(result2.Count == 2);


            // Test 2
            // *["Two" in tags]
            // .Where(p => p.Ids.Contains(ids))
            var result3 = await categories.Where(c => c.Tags.Contains("Two")).ToListAsync();
            Assert.True( result3.Count == 1);

            var result4 = await categories.Where(c => c.Numbers.Contains(3)).ToListAsync();
            Assert.True(result4.Count == 1);

            var result5 = await categories.Where(c => c.Numbers.Contains(c.InternalId)).ToListAsync();
            Assert.True(result5.Count == 1);

        }

        [Fact]
        public async Task CRUD_Test()
        {

            var sanity = new SanityDataContext(Options);

            await ClearAllDataAsync(sanity);

            // Create new category
            var categories = sanity.DocumentSet<Category>();

            var category1 = new Category
            {
                CategoryId = Guid.NewGuid().ToString(),
                Description = "Some of world's greatest conventions!",
                Title = "Convention"
            };
            var category2 = new Category
            {
                CategoryId = Guid.NewGuid().ToString(),
                Description = "Excitement, fun and relaxation...",
                Title = "Resort"
            };
            var category3 = new Category
            {
                CategoryId = Guid.NewGuid().ToString(),
                Description = "World class hotel rooms, restaurants and facilities...",
                Title = "Hotel"
            };

            // Chained mutations
            categories.Create(category1).Create(category2).Create(category3);

            
            // Create new author
            var author = new Author
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Joe Bloggs",
                Slug = new SanitySlug("joe"),
                FavoriteCategories = new List<SanityReference<Category>>()
                {
                    new SanityReference<Category>
                    {
                        Value = category1
                    }
                }
            };

            sanity.DocumentSet<Author>().Create(author);

            // Create post
            var post = new Post
            {
                Title = "Welcome to Oslofjord Convention Center!",
                PublishedAt = DateTimeOffset.Now,
                Categories = new List<SanityReference<Category>>
                {
                    new SanityReference<Category>
                    {
                        Ref = category1.CategoryId
                    },
                    new SanityReference<Category>
                    {
                        Ref = category2.CategoryId
                    },
                    new SanityReference<Category>
                    {
                        Ref = category3.CategoryId
                    }
                },
                Author = new SanityReference<Author>
                {
                    Value = author
                },
                Body = new[]
                {
                    new SanityBlock
                    {
                        Children = new []
                        {
                            new SanitySpan
                            {
                                Text = "A bold start!",
                                Marks = new[] { "strong" }
                            }
                        }
                    },
                    new SanityBlock
                    {
                        Children = new []
                        {
                            new SanitySpan
                            {
                                Text = "With a great article..."
                            }
                        }
                    }
                }
            };
            sanity.DocumentSet<Post>().Create(post);

            // Save all changes in one transaction
            var result = await sanity.CommitAsync();


            // Retreive all categories (recursive structure)
            var allCategories = await sanity.DocumentSet<Category>().ToListAsync();
            Assert.True(allCategories.Count >= 3);


            // LINQ Query
            var count = (sanity.DocumentSet<Post>().Count());
            var query = sanity.DocumentSet<Post>()
                .Include(p => p.Author)
                .Include(p => p.DereferencedAuthor, "author")
                .Include(p => p.Author.Value.Images)
                .Include(p => p.Categories)
                .Include(p => p.Author.Value.FavoriteCategories)
                .Where(p => p.PublishedAt >= DateTime.Today);

            // Execute Query
            var results = await query.ToListAsync();

            var transformedResult = sanity.DocumentSet<Post>().Where(p => !p.IsDraft()).Select(p => new { p.Title, p.Author.Value.Name }).ToList();

            Assert.True(count > 0);
            Assert.True(results.Count > 0);
            Assert.NotNull(results[0].Author?.Value);
            Assert.NotNull(results[0].DereferencedAuthor);
            Assert.Equal("Joe Bloggs", results[0].Author.Value.Name);

            // Update test
            var postToUpdate = results[0];
            postToUpdate.Title = "New title";

            await sanity.DocumentSet<Post>().Update(postToUpdate).CommitAsync();

            var updatedItem = await sanity.DocumentSet<Post>().GetAsync(postToUpdate.Id);

            Assert.True(updatedItem.Title == "New title");
        }
    }
}
