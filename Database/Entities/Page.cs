using System.Collections.Generic;

namespace Inshapardaz.DataImport.Database.Entities
{
    public class Page<T>
    {
        public int PageNumber { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }

        public IEnumerable<T> Data { get; set; }
    }
}
