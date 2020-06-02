using System.Collections.Generic;

namespace iess_api.Models
{
    public class PageResponse<T>
    {
        public int TotalPages { get; set; }

        public int TotalItems { get; set; }

        public int CurrentPage { get; set; }

        public int ItemsPerPage { get; set; }

        public IEnumerable<T> Items { get; set; }
    }
}