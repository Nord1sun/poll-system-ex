using iess_api.Interfaces;
using iess_api.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace iess_api.Repositories
{
    public class ExceptionRepository : RepositoryBase<ExceptionModel>, IExceptionRepository
    {
        public ExceptionRepository(IOptions<MongoDbSettings> settings) : base(settings) { }

        public override string CollectionName => "Exceptions";
    }
}
