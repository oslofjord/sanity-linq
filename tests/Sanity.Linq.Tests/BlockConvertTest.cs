using Sanity.Linq.BlockContent;
using Sanity.Linq.CommonTypes;
using Sanity.Linq.Demo.Model;
using Sanity.Linq.Extensions;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Sanity.Linq.Tests
{
    public class BlockConvertTest : TestBase
    {
        [Fact]
        public async Task ConvertTest()
        {
            // this test tries to serialize the Body-element of a Post-document from the Sanity demo-dataset.
            var sanity = new SanityDataContext(Options);
            var posts = await sanity.DocumentSet<Post>().ToListAsync();
            var htmlBuilder = new SanityHtmlBuilder(Options);
            foreach (var post in posts)
            {
                var html = htmlBuilder.BuildAsync(post.Body, null); // the serialized data
            }
        }

        [Fact]
        public async Task BlockContent_Extensions_Test()
        {
            var sanity = new SanityDataContext(Options);

            // Clear existing records in single transaction
            await ClearAllDataAsync(sanity);

            // Uplooad image
            var imageUri = new Uri("https://www.sanity.io/static/images/opengraph/social.png");
            var image = (await sanity.Images.UploadAsync(imageUri)).Document;

            // Create post
            var post = new Post
            {
                MainImage = new SanityImage
                {
                    Asset = new SanityReference<SanityImageAsset> { Ref = image.Id },
                },

                Body = new SanityObject[]
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
                    },
                     new SanityImage
                    {
                        Asset = new SanityReference<SanityImageAsset> { Ref = image.Id },
                    },
                     new SanityBlock
                     {
                         SanityType = "block",
                         Level = 1,
                         ListItem = "bullet",
                         Children = new []
                            {
                                new SanitySpan
                                {
                                    Text = "With a great article..."
                                }
                            }
                     },
                     new SanityBlock
                     {
                         SanityType = "block",
                         Level = 1,
                         ListItem = "bullet",
                         Children = new []
                            {
                                new SanitySpan
                                {
                                    Text = "With a great article..."
                                }
                            }
                     },
                     new SanityBlock
                     {
                         SanityType = "block",
                         Level = 2,
                         ListItem = "bullet",
                         Children = new []
                            {
                                new SanitySpan
                                {
                                    Text = "With a great article..."
                                }
                            }
                     },
                     new SanityBlock
                     {
                         SanityType = "block",
                         Level = 3,
                         ListItem = "bullet",
                         Children = new []
                            {
                                new SanitySpan
                                {
                                    Text = "With a great article..."
                                }
                            }
                     },
                     new SanityBlock
                     {
                         SanityType = "block",
                         Level = 1,
                         ListItem = "number",
                         Children = new []
                            {
                                new SanitySpan
                                {
                                    Text = "With a great article..."
                                }
                            }
                     }
                     ,
                     new SanityBlock
                     {
                         SanityType = "block",
                         Level = 2,
                         ListItem = "number",
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
            var result = (await sanity.DocumentSet<Post>().Create(post).CommitAsync()).Results[0].Document;

            // Serialize block content
            var html = await result.Body.ToHtmlAsync(sanity);

            // Serialize single object
            var imageTag = await result.MainImage.ToHtmlAsync(sanity);

            //test whole content
            Assert.NotNull(html);
            Assert.True(html.IndexOf("<strong>A bold start!</strong>") != -1);
            Assert.True(html.IndexOf("<img") != -1);
            Assert.True(html.IndexOf(image.AssetId) != -1);

            //test image
            Assert.True(imageTag.IndexOf("<img") != -1);
            Assert.True(imageTag.IndexOf(image.AssetId) != -1);
        }
    }
}
