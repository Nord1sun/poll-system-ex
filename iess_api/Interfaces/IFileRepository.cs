using System.Collections.Generic;
using System.Threading.Tasks;
using iess_api.Constants;
using iess_api.Models;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver.GridFS;

namespace iess_api.Interfaces
{
    public interface IFileRepository
    {
        GridFSBucket Bucket { get; }
        Task<string> UploadAsync(IFormFile file);
        Task<GridFSDownloadStream> DownloadAsync(string id);
        Task<bool> ExistsAsync(string id);
        Task DeleteAsync(string id);
        Task<List<GridFSFileInfo>> GetAllFiles();
        Task<bool> AssociatePicturesWithPoll<TQuestion>(PollBase<TQuestion> pollBase, PicturesAssociateMode mode) where TQuestion:Question;
        void DeletePollFromMetadata(string pictureId, string pollId);
    }
}