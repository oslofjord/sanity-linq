using Sanity.Linq.CommonTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sanity.Linq.Demo.Model
{
    public class Table : SanityDocument
    {
        public Table() : base()
        {
            
        }

        public string Title { get; set; }

        public bool Bootstrap { get; set; }

        //TODO: add bootstrap options

        public List<TableRow> Rows { get; set; }

    }

    public class TableRow : SanityObject
    {
        public string[] Cells { get; set; }
    }
}
