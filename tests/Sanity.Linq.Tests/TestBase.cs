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
        public SanityOptions Options => new SanityOptions
        {
            ProjectId = "dnjvf98k",
            Dataset = "test",
            Token = "skTl7VigmZzLpK4d6qE3hgeBqnTHILh5v9nSOX689Nk3bcd2Xs1Mm1rXt7JxWSBsBcrXmc5omCHd63kjYUDaCs0k1DNTz1qrIT5MX6I66Lsr9XD1Ln3NNaomZWFIBoIw1Y0bnwVSTgsDUR4BRqfO8bCXTfzFRvIBZdgwJxcRV8isJzbFmQJ7",
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
