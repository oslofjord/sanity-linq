using Sanity.Linq.Demo.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Sanity.Linq.Tests
{
    public class TestBase
    {
        // Credentials come from environment variables (set locally or via CI secrets).
        public SanityOptions Options => new SanityOptions
        {
            ProjectId = Environment.GetEnvironmentVariable("SANITY_PROJECT_ID"),
            Dataset = Environment.GetEnvironmentVariable("SANITY_DATASET"),
            Token = Environment.GetEnvironmentVariable("SANITY_TOKEN"),
            UseCdn = false
        };

        public async Task ClearAllDataAsync(SanityDataContext sanity)
        {
            // Clear existing records in single transaction
            sanity.DocumentSet<Post>().Delete();
            sanity.DocumentSet<Author>().Delete();
            sanity.DocumentSet<Category>().Delete();
            await sanity.CommitAsync();

            await sanity.Images.Delete().CommitAsync();
        }
    }
}
