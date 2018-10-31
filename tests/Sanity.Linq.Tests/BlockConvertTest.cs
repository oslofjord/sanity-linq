using Newtonsoft.Json.Linq;
using Sanity.Linq.BlockContent;
using Sanity.Linq.Demo.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Sanity.Linq.Tests
{
    public class BlockConvertTest : TestBase
    {
        HtmlBuilder htmlBuilder;

        [Fact]
        public async Task ConvertTest()
        {
            var sanity = new SanityDataContext(Options);
            var posts = await sanity.DocumentSet<Post>().ToListAsync();
            var htmlBuilder = new HtmlBuilder(Options);
            foreach (var post in posts)
            {
                var html = htmlBuilder.Build(post.Body);
            }
        }



        //public Dictionary<string, Func<string, string>> customSerializers()
        //{
        //    var serializers = new Dictionary<string, Func<TType, string>>()
        //      {
        //        {
        //            "author",
        //            delegate (string author) {
        //                return "<div>" + author['attributes']['name'] + "</div>";
        //            }
        //        },
        //          {
        //            "block",
        //            delegate (bool value) {
        //                return value;
        //            } }
        //      };

        //    return serializers;
        //}
        //}

        //class GenericFuncDictionary<T> : Dictionary<string, Func<T, string>>
        //{
        //    public void returnValues()
        //    {
        //        foreach (Func<T> fun in this.Values)
        //            Console.WriteLine(fun());
        //    }

        //    public string returnAuthor()
        //    {
        //        if (T == Array)
        //        return "<div>" + author['attributes']['name'] + "</div>";
        //    }
        //}
    }
}
