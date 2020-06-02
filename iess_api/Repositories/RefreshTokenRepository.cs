using iess_api.Interfaces;
using iess_api.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace iess_api.Repositories
{
    public class RefreshTokenRepository : RepositoryBase<RefreshTokenModel>, IRefreshTokenRepository
    {
        public RefreshTokenRepository(IOptions<MongoDbSettings> settings) : base(settings) { }
        public override string CollectionName => "RefreshTokens";

        public async Task<RefreshTokenModel> GetRefreshToken(string token)
        {
            var cursor = await Collection.FindAsync(Builders<RefreshTokenModel>.Filter.Eq("Token", token));
            return await cursor.FirstOrDefaultAsync();
        }

        public async Task<RefreshTokenModel> GetRefreshTokenByUsername(string username)
        {
            var cursor = await Collection.FindAsync(Builders<RefreshTokenModel>.Filter.Eq("Username", username));
            return await cursor.FirstOrDefaultAsync();
        }
    }
}
