using System.Threading.Tasks;
using iess_api.Interfaces;
using iess_api.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace iess_api.Repositories
{
    public class TokenRepository : UserRepository, ITokenRepository
    {
        public TokenRepository(IOptions<MongoDbSettings> settings) : base(settings) { }

        public override string CollectionName => "Users";

        public async Task<UserModel> GetPersonAsync(string username, string password)
        {
            var cursor = await Collection.FindAsync(x => x.Login == username && x.PasswordHash == password);
            return await cursor.FirstOrDefaultAsync();
        }

        public async Task<UserModel> GetPersonByNameAsync(string username)
        {
            var cursor = await Collection.FindAsync(x => x.Login == username);
            return await cursor.FirstOrDefaultAsync();
        }
        
    }
}
 