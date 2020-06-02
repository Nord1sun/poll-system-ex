using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using iess_api.Models;
using iess_api.Repositories;
using MongoDB.Driver;

namespace iess_api.Interfaces
{
    public interface IPollBaseRepository<TPoll,TAnswersUnit> 
    {
        IMongoCollection<TPoll> PollBaseCollection { get; }

        Task<PageResponse<BriefPollBaseResponse>> GetAllAsync(PageInfo info,string senderId);

        Task<PageResponse<HistoryPollBaseResponse>> GetAllAnsweredByUser(string userId, PageInfo info);
            
        Task<List<BriefPollBaseResponse>> GetAllAvailableForUserAsync(string userId);

        Task<List<BriefPollBaseResponse>> GetAllActiveByTeacher(string teacherId);

        Task AddAsync(TPoll pollBase);

        Task<TPoll> GetByIdAsync(string id);

        Task<TPoll> GetPollBaseByQuestionIdAsync(string id);

        Task DeleteAsync(string id);
        Task<bool> ExistsAsync(string id);

        Task UpdateAsync(TPoll newPollBase);
        
        Task<long> StartAsync(string id, DateTime expireDate);

        Task<long> StopAsync(string id);

        Task<bool> CheckUserCanAnswer(string pollBaseId, string userId);
        
        Task AddAnswersUnitAsync(TAnswersUnit answer);

        Task UpdateAnswersUnitAsync(TAnswersUnit newAnswersUnit,TAnswersUnit oldAnswersUnit);
        
        Task DeleteAnswersUnitAsync(string id);

        Task<TAnswersUnit> GetAnswersUnitByIdAsync(string id);

        Task<TAnswersUnit> GetAnswersUnitForUserAsync(string pollBaseId, string userId);

        Task<TAnswersUnit> GetUserPreviousAnswer(string pollBaseId, string userId);

        Task<IEnumerable<TAnswersUnit>> GetPollBaseAnswersAsync(string pollBaseId);

        Task<IEnumerable<TAnswersUnit>> GetAllAnswersUnitsAsync();
    }
}