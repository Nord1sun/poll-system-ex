using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using iess_api.Constants;
using iess_api.Interfaces;
using iess_api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace iess_api.Repositories
{
    public class GridFsRepository : IFileRepository
    {
        private readonly IMongoDatabase _db;
        public GridFSBucket Bucket { get; }

        public GridFsRepository(IOptions<MongoDbSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            _db = client.GetDatabase(settings.Value.Database);
            Bucket = new GridFSBucket(_db);
        }

        public async Task<string> UploadAsync(IFormFile file)
        {
            var options = new GridFSUploadOptions
            {
                Metadata = new BsonDocument
                {
                    {"contentType", file.ContentType },
                    {"related_polls",new BsonArray() }
                }
            };
            var fileId = await Bucket.UploadFromStreamAsync(file.FileName, file.OpenReadStream(),options);
            return fileId.ToString();
        }

        public async Task<GridFSDownloadStream> DownloadAsync(string id)
        {
            ObjectId.TryParse(id, out ObjectId oid);
            return await Bucket.OpenDownloadStreamAsync(oid,new GridFSDownloadOptions(){Seekable = true});
        }

        public async Task<bool> ExistsAsync(string id)
        {
            ObjectId.TryParse(id, out ObjectId oid);
            var filter = Builders<GridFSFileInfo>.Filter.Eq("_id",oid);
            return await Bucket.Find(filter).AnyAsync();
        }

        public async Task DeleteAsync(string id)
        {
            ObjectId.TryParse(id, out ObjectId oid);
            await Bucket.DeleteAsync(oid);
        }

        public async Task<List<GridFSFileInfo>> GetAllFiles()
        {
            var filter = Builders<GridFSFileInfo>.Filter.Empty;
            return await Bucket.Find(filter).ToListAsync(); 
        }

        public async Task<bool> AssociatePicturesWithPoll<TQuestion>(PollBase<TQuestion> pollBase,PicturesAssociateMode mode) where TQuestion:Question
        {
            var pollId = pollBase.Id;
            var resultsTasks = pollBase.Questions.Select(async question =>
            {
                var questionResultsTasks = question.AnswerOptions!=null?
                    question.AnswerOptions.Select(async option =>
                    option.PictureId != null ? await UpdatePictureMetadataAsync(option.PictureId, pollId, mode) : null).ToList()
                    :new List<Task<UpdateResult>>();
                var questionResults = (await Task.WhenAll(questionResultsTasks)).ToList();
                questionResults.Add(question?.PictureId != null ? await UpdatePictureMetadataAsync(question.PictureId, pollId, mode) : null);
                return questionResults;
            });
            var results=(await Task.WhenAll(resultsTasks)).SelectMany(list=>list).ToList();
            return results.Where(result=>result!=null).All(result => result.ModifiedCount == 1);
        }

        private async Task<BsonArray> GetNewPictureMetadataAsync(string id,string pollId, PicturesAssociateMode mode)
        {
            ObjectId.TryParse(id, out ObjectId oid);
            var filter = Builders<GridFSFileInfo>.Filter.Eq("_id", oid);
            var fileInfo = await Bucket.Find(filter).SingleAsync();
            var bsonArray = fileInfo.Metadata["related_polls"].AsBsonArray;
            switch (mode)
            {
                case PicturesAssociateMode.Link:
                    bsonArray.Add(pollId);
                    break;
                case PicturesAssociateMode.Unlink:
                    bsonArray.Remove(pollId);
                    break;
            }
            return bsonArray;
        }

        private async Task<UpdateResult> UpdatePictureMetadataAsync(string pictureId, string pollId, PicturesAssociateMode mode)
        {
            if (! await ExistsAsync(pictureId))
                return null;
            var filesCollection = _db.GetCollection<BsonDocument>("fs.files");
            ObjectId.TryParse(pictureId, out ObjectId oid);
            var filter = Builders<BsonDocument>.Filter.Eq("_id", oid);
            var bsonArray = await GetNewPictureMetadataAsync(pictureId, pollId,mode);
            if (bsonArray.Count == 0)
            {
                await DeleteAsync(pictureId);
                return null;
            }
            var update = Builders<BsonDocument>.Update.Set("metadata.related_polls", bsonArray);
            return await filesCollection.UpdateOneAsync(filter, update);
        }

        public void DeletePollFromMetadata(string pictureId, string pollId)
        {
            UpdatePictureMetadataAsync(pictureId, pollId, PicturesAssociateMode.Unlink).Wait();
        }
    }
}
