using System.Collections.Generic;

namespace iess_api.Models
{
    public class PageInfo
    {
        public int CurrentPage { get; set; }
        
        public int ItemsPerPage { get; set; }

        public string OrderBy { get; set; }

        public string Order { get; set; }

        public string Filter { get; set; }

        public bool? CreatedBySender  { get; set; }

        public List<string> DateLabels { get; set; }
    }
}