using Sanity.Linq.CommonTypes;
using Sanity.Linq.Demo.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Sanity.Linq.Tests
{
    public class CommonTypeTest : TestBase
    {

        [Fact]
        public async Task SanityLocaleString_GetWithLangugeCode_ShouldReturnAValue()
        {
            var sanity = new SanityDataContext(Options);
            await ClearAllDataAsync(sanity);

            var page = new Page
            {
                Id = Guid.NewGuid().ToString(),
                Title = new SanityLocaleString(),
            };

            page.Title.Set("en", "My Page");
            page.Title.Set("no", "Min side");

            // Create page
            await sanity.DocumentSet<Page>().Create(page).CommitAsync();

            // Retrieve newly created page
            page = await sanity.DocumentSet<Page>().GetAsync(page.Id);

            var enTitle = page.Title.Get("en");
            var noTitle = page.Title.Get("no");

            Assert.NotNull(enTitle);
            Assert.NotNull(noTitle);
            Assert.Equal("My Page", enTitle);
            Assert.Equal("Min side", noTitle);



        }

        [Fact]
        public async Task SanityLocaleT_GetWithLangugeCode_ShouldReturnAT()
        {
            var sanity = new SanityDataContext(Options);
            await ClearAllDataAsync(sanity);

            var page = new Page
            {
                Id = Guid.NewGuid().ToString(),
                Options = new SanityLocale<PageOptions>()
            };

            page.Options.Set("en", new PageOptions { ShowOnFrontPage = false, Subtitle = "Awesome page" });
            page.Options.Set("no", new PageOptions { ShowOnFrontPage = true, Subtitle = "Heftig bra side!" });

            // Create page
            await sanity.DocumentSet<Page>().Create(page).CommitAsync();

            // Retrieve newly created page
            page = await sanity.DocumentSet<Page>().GetAsync(page.Id);

            var enOptions = page.Options.Get("en");
            var noOptions = page.Options.Get("no");

            Assert.NotNull(enOptions);
            Assert.NotNull(noOptions);
            Assert.Equal("Awesome page", enOptions.Subtitle);
            Assert.Equal("Heftig bra side!", noOptions.Subtitle);
            Assert.False(enOptions.ShowOnFrontPage);
            Assert.True(noOptions.ShowOnFrontPage);
        }


    }
}
