using iess_api.Models;
using iess_api.Interfaces;
using Microsoft.Extensions.Options;

namespace iess_api.Repositories
{
    public class GroupRepository : RepositoryBase<GroupModel>, IGroupRepository
    {
        public override string CollectionName => "Groups";

        public GroupRepository(IOptions<MongoDbSettings> options) : base(options) { }
    }
}
