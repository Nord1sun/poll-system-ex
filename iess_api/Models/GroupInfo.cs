using System;
using System.Collections.Generic;

namespace iess_api.Models
{
    public class GroupInfo
    {
        public string Id { get; set; }
        
        public string Name { get; set; }

        public int TotalUsers { get; set; }

        public IEnumerable<UserModel> Users { get; set; }
    }
}
