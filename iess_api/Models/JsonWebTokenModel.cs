using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace iess_api.Models
{
    public class JsonWebTokenModel
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
}
