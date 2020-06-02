using System;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using iess_api.Models;
using iess_api.Interfaces;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;


namespace iess_api.Repositories
{
    public class UserRepository : RepositoryBase<UserModel>, IUserRepository
    {
        public override string CollectionName => "Users";
        
        public UserRepository(IOptions<MongoDbSettings> options) : base(options) { }

        public async Task<PageResponse<UserModel>> GetAllUsersAsync(PageInfo info)
        {
            var filterBuilder = Builders<UserModel>.Filter;
            var filter = FilterDefinition<UserModel>.Empty;

            if (info.Filter != null) 
            {
                filter = filterBuilder.Where(x => x.Login.ToUpper().Contains(info.Filter));
                filter |= filterBuilder.Where(x => x.Role.ToUpper().Contains(info.Filter));
                filter |= filterBuilder.Where(x => x.GroupName.ToUpper().Contains(info.Filter));
                filter |= filterBuilder.Where(x => x.FirstName.ToUpper().Contains(info.Filter));
                filter |= filterBuilder.Where(x => x.LastName.ToUpper().Contains(info.Filter));
            }

            var sortBuilder = Builders<UserModel>.Sort;
            SortDefinition<UserModel> sortDefinition = null;
            info.Order = info.Order ?? "login";

            if (info.OrderBy == null || info.OrderBy.Equals("ASCENDING"))
            {
                sortDefinition = sortBuilder.Ascending(info.Order);
            }
            else if (info.OrderBy != null && info.OrderBy.Equals("DESCENDING"))
            {
                sortDefinition = sortBuilder.Descending(info.Order);
            }

            var cursor = await Collection.FindAsync(filter, new FindOptions<UserModel, UserModel> { Sort = sortDefinition });
            var users = await cursor.ToListAsync();
            var size = users.Count();

            return new PageResponse<UserModel>() {
                CurrentPage = info.CurrentPage,
                TotalItems = size,
                TotalPages = (int)Math.Ceiling((double)size / (double)info.ItemsPerPage),
                ItemsPerPage = info.ItemsPerPage,
                Items = users.Select(model => new UserModel {
                   UserId = model.UserId,
                   Login = model.Login,
                   FirstName = model.FirstName,
                   LastName = model.LastName,
                   GroupName = model.GroupName,
                   PasswordHash = null,
                   Role = model.Role
                }).Skip((info.CurrentPage - 1) * info.ItemsPerPage).Take(info.ItemsPerPage)
            };
        }

        public async Task UpdateLastLogin(string id)
        {
            var user = await base.GetAsync(id);
            user.LastLogin = DateTime.Now;
            var result = await base.ReplaceAsync(user, x => x.UserId.Equals(user.UserId));
        }
    }
}
