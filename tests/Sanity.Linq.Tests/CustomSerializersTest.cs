using Sanity.Linq.Demo.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Sanity.Linq.Tests
{ 
    public class CustomSerializersTest : TestBase
    {

        [Fact]
        public async Task SerializeToBootstrapTable()
        {
            // Test of DataTable Object https://github.com/fredjens/sanity-datatable

            var post = new Table
            {
                Title = "Test Table",
                Bootstrap = false,
                Rows = new List<TableRow>()
                {
                    new TableRow()
                    {
                         Cells = new string[] {"", "", ""}        //first row is headers
                    }
                }
            };





            //get document from sanity
            //build
            //set marks
            //return html


        }


    }
}
