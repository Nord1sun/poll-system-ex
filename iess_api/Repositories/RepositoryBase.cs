using System;
using MongoDB.Bson;
using MongoDB.Driver;
using iess_api.Models;
using iess_api.Interfaces;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace iess_api.Repositories
{
    public abstract class RepositoryBase<T> : IRepositoryBase<T> where T : class
    {
        public abstract string CollectionName { get; }
        protected IMongoDatabase MongoDatabase { get; }
        protected IMongoCollection<T> Collection => MongoDatabase.GetCollection<T>(CollectionName);

        protected RepositoryBase(IOptions<MongoDbSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            MongoDatabase = client.GetDatabase(settings.Value.Database);
        }

        public async Task<bool> DeleteAsync(Expression<Func<T, bool>> expression)
        {
            var deleteResult = await Collection.DeleteOneAsync(expression);
            return deleteResult.DeletedCount > 0;
        }

        public async Task<List<T>> GetAllAsync()
        {
            var cursor = await Collection.FindAsync(FilterDefinition<T>.Empty);
            return await cursor.ToListAsync();
        }

        public async Task<T> GetAsync(string id)
        {
            var cursor = await Collection.FindAsync(Builders<T>.Filter.Eq("_id", new ObjectId(id)));
            return await cursor.FirstOrDefaultAsync();
        }

        public async Task<T> PostAsync(T obj)
        {
            await Collection.InsertOneAsync(obj);
            return obj;
        }

        public async Task<bool> ReplaceAsync(T obj, Expression<Func<T, bool>> expression)
        {
            var result = await Collection.ReplaceOneAsync(expression, obj);
            return result.ModifiedCount > 0;
        }
    }
}
