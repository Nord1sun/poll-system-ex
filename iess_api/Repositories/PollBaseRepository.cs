using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using iess_api.Constants;
using iess_api.Interfaces;
using iess_api.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace iess_api.Repositories
{
    public abstract class PollBaseRepository<TPoll,TQuestion,TAnswersUnit, TAnswer> : IPollBaseRepository<TPoll,TAnswersUnit> 
        where TPoll : PollBase<TQuestion>
        where TQuestion :Question
        where TAnswersUnit:AnswersUnit<TAnswer>
        where TAnswer : Answer
    {
        private readonly IFileRepository _pictureRepository;
        private readonly IGroupRepository _groupRepository;
        private readonly IMongoDatabase _db;
        public IMongoCollection<TPoll> PollBaseCollection => _db.GetCollection<TPoll>(typeof(TPoll).Name);
        public IMongoCollection<TAnswersUnit> AnswersUnits => _db.GetCollection<TAnswersUnit>(typeof(TAnswersUnit).Name);
        public IMongoCollection<UserModel> Users => _db.GetCollection<UserModel>("Users");

        protected PollBaseRepository(IOptions<MongoDbSettings> settings,IFileRepository pictureRepository, IGroupRepository groupRepository)
        {
            _pictureRepository = pictureRepository;
            _groupRepository = groupRepository;
            var client = new MongoClient(settings.Value.ConnectionString);
            _db = client.GetDatabase(settings.Value.Database);
        }
        
        private async Task<PageResponse<T>> FilterAndSortQuery<T>(IMongoQueryable<T> query, PageInfo info,string senderId) where T:ISupportFiltering
        {
            if (!string.IsNullOrWhiteSpace(info.Order) && !string.IsNullOrWhiteSpace(info.OrderBy))
            {
                if (typeof(T).GetProperty(info.Order,BindingFlags.IgnoreCase |  BindingFlags.Public | BindingFlags.Instance) != null)
                    query = info.OrderBy == "ASCENDING"
                        ? query.OrderBy(GetPropertySelector<T>(info.Order))
                        : query.OrderByDescending(GetPropertySelector<T>(info.Order)) ;
            }
            var responses = await query.ToListAsync();
            if (info.CreatedBySender.HasValue && info.CreatedBySender.Value)
                responses = responses.Where(response => response.CheckCreator(senderId)).ToList();
            if (info.DateLabels!=null)
            {
                responses = responses.Where(response => response.CheckDateStatus(info.DateLabels.Select(l=>l.ToLower()).ToList())).ToList();
            }
            if (!string.IsNullOrWhiteSpace(info.Filter))
            {
                var filters = new Regex(@"\s").Split(info.Filter.ToLower());
                var groupfilters = new List<string>();
                var textsfilter= new List<string>();
                var groupNames = (await _groupRepository.GetAllAsync()).Select(group=>group.Name).ToList();
                bool groupflag;
                bool textflag;
                var filteredResponses=new List<T>();
                foreach (var filter in filters)
                {
                    if(groupNames.Contains(filter))
                        groupfilters.Add(filter);
                    else 
                        textsfilter.Add(filter);
                }
                foreach (var response in responses)
                {
                    groupflag = true;
                    textflag = true;
                    if (groupfilters.Count!=0)
                    {
                        groupflag=response.CheckGroupFilters(groupfilters);
                    }
                    if (textsfilter.Count!=0)
                    {
                        foreach (var filter in textsfilter)
                        {
                            textflag = response.CheckTextFilter(filter);
                            if(!textflag)
                                break;
                        }
                    }
                    if (groupflag&&textflag)
                    {
                        filteredResponses.Add(response);
                    }
                }
                responses = filteredResponses;
            }
            return new PageResponse<T>
            {
                CurrentPage=info.CurrentPage,
                ItemsPerPage = info.ItemsPerPage,
                TotalItems = responses.Count,
                TotalPages = (int)Math.Ceiling((double)responses.Count / info.ItemsPerPage),
                Items = responses.Skip((info.CurrentPage - 1) * info.ItemsPerPage).Take(info.ItemsPerPage)
            };
        }

        private static Expression<Func<T, object>> GetPropertySelector<T>(string propertyName)
        {
            var arg = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(arg, propertyName);
            var conv = Expression.Convert(property, typeof(object));
            var exp = Expression.Lambda<Func<T, object>>(conv, arg);
            return exp;
        }

        public async Task<PageResponse<BriefPollBaseResponse>> GetAllAsync(PageInfo info,string senderId)
        {
            var query = from pollBase in PollBaseCollection.AsQueryable()
                join creatorUserModel in Users.AsQueryable()
                    on pollBase.CreatorUserId equals creatorUserModel.UserId
                select new BriefPollBaseResponse
                {
                    Id = pollBase.Id,
                    Title = pollBase.Title,
                    StartDate = pollBase.StartDate,
                    ExpireDate = pollBase.ExpireDate,
                    EligibleGroups = pollBase.EligibleGroupsNames,
                    FirstName = creatorUserModel.FirstName,
                    LastName = creatorUserModel.LastName,
                    CreatorUserId = pollBase.CreatorUserId,
                    IsAllowedToReanswer=pollBase.IsAllowedToReanswer,
                    AreStatsPublic=pollBase.AreStatsPublic
                };
            return await FilterAndSortQuery(query, info,senderId);
        }
        
        //forme
        public virtual async Task<List<BriefPollBaseResponse>> GetAllAvailableForUserAsync(string userId)
        {
            var user = await Users.Find(userModel => userModel.UserId == userId).SingleAsync();
            var query = PollBaseCollection.AsQueryable()
                .Where(pollBase=>pollBase.EligibleGroupsNames.Any(group=>group==user.GroupName)&&pollBase.StartDate<DateTime.Now&&pollBase.ExpireDate>DateTime.Now)
                .Join(Users.AsQueryable(), pollBase => pollBase.CreatorUserId, creatorUserModel => creatorUserModel.UserId,
                    (pollBase, creatorUserModel) => new BriefPollBaseResponse
                    {
                        Id = pollBase.Id,
                        Title = pollBase.Title,
                        StartDate = pollBase.StartDate,
                        ExpireDate = pollBase.ExpireDate,
                        EligibleGroups = pollBase.EligibleGroupsNames,
                        FirstName = creatorUserModel.FirstName,
                        LastName = creatorUserModel.LastName,
                        CreatorUserId = pollBase.CreatorUserId,
                        IsAllowedToReanswer=pollBase.IsAllowedToReanswer,
                        AreStatsPublic=pollBase.AreStatsPublic
                    });
            var list=await query.ToListAsync();
            var resultList = new List<BriefPollBaseResponse>();
            foreach (var response in list)
            {
                var answersUnit=AnswersUnits.AsQueryable().SingleOrDefault(unit => unit.CreatorUserId == userId && unit.PollBaseId == response.Id);
                if (answersUnit != null)
                {
                    if ((bool) !response.IsAllowedToReanswer)
                        continue;
                    response.IsAnswerCompleted=answersUnit.IsCompleted;
                }

                response.HasAnswer = answersUnit != null;
                resultList.Add(response);
            }
            return resultList;
        }

        //history
        public virtual async Task<PageResponse<HistoryPollBaseResponse>> GetAllAnsweredByUser(string userId,PageInfo info)
        {
            var query = AnswersUnits.AsQueryable()
                .Where(answer => answer.CreatorUserId == userId)
                .Join(PollBaseCollection.AsQueryable(), answersUnit => answersUnit.PollBaseId, pollBase => pollBase.Id,
                    (answersUnit,pollBase)=> new HistoryPollBaseResponse
                    {
                        Id = pollBase.Id,
                        Title = pollBase.Title,
                        StartDate = pollBase.StartDate,
                        ExpireDate = pollBase.ExpireDate,
                        EligibleGroups = pollBase.EligibleGroupsNames,
                        PollBaseCreatorId = pollBase.CreatorUserId,
                        AnswerId = answersUnit.Id,
                        IsCompleted = answersUnit.IsCompleted,
                        AnswerDate = answersUnit.AnswerDate
                    })
                .Join(Users.AsQueryable(), response => response.PollBaseCreatorId, user => user.UserId, (response, user) =>new HistoryPollBaseResponse
                {
                    Id = response.Id,
                    Title = response.Title,
                    StartDate = response.StartDate,
                    ExpireDate = response.ExpireDate,
                    EligibleGroups = response.EligibleGroups,
                    PollBaseCreatorId = response.PollBaseCreatorId,
                    AnswerId = response.AnswerId,
                    IsCompleted = response.IsCompleted,
                    AnswerDate = response.AnswerDate,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                });
            return await FilterAndSortQuery(query, info,userId);
        }

        //forme
        public async Task<List<BriefPollBaseResponse>> GetAllActiveByTeacher(string teacherId)
        {
            return await PollBaseCollection.AsQueryable()
                .Where(pollBase => pollBase.CreatorUserId == teacherId && pollBase.StartDate < DateTime.Now &&pollBase.ExpireDate > DateTime.Now)
                .Select(pollBase => new BriefPollBaseResponse
                {
                    Id = pollBase.Id,
                    Title = pollBase.Title,
                    StartDate = pollBase.StartDate,
                    ExpireDate = pollBase.ExpireDate,
                    EligibleGroups = pollBase.EligibleGroupsNames,
                    IsAllowedToReanswer = pollBase.IsAllowedToReanswer,
                    AreStatsPublic = pollBase.AreStatsPublic
                }).ToListAsync();
        }

        public virtual async Task<TPoll> GetByIdAsync(string id)
        { 
            return await PollBaseCollection.Find(pollBase => pollBase.Id == id).FirstOrDefaultAsync();
        }
        
        public async Task<TPoll> GetPollBaseByQuestionIdAsync(string id)
        {
            return await PollBaseCollection.Find(pollBase => pollBase.Questions.Any(question=>question.Id == id)).FirstOrDefaultAsync();
        }
        
        public virtual async Task AddAsync(TPoll pollBase)
        {
            pollBase.Id = null;
            pollBase.CreationDate = DateTime.Now;
            pollBase.AreStatsPublic = true;

            if (pollBase is Poll poll)
                poll.AreAnswersAnonymous = true;

            await PollBaseCollection.InsertOneAsync(pollBase);
        }

        public async Task DeleteAsync(string id)
        {
            var pollBase = await GetByIdAsync(id);
            await AnswersUnits.DeleteOneAsync(answersUnit=> answersUnit.PollBaseId==id);
            await _pictureRepository.AssociatePicturesWithPoll(pollBase, PicturesAssociateMode.Unlink);
            await PollBaseCollection.DeleteOneAsync(poll => poll.Id == id);
        }

        //Question.Id's wil be overwritten by this
        public virtual async Task UpdateAsync(TPoll newPollBase)
        {
            var oldPollBase = await GetByIdAsync(newPollBase.Id);

            newPollBase.Id = oldPollBase.Id;
            newPollBase.CreationDate = oldPollBase.CreationDate;
            newPollBase.CreatorUserId = oldPollBase.CreatorUserId;

            await PollBaseCollection.DeleteOneAsync(poll => poll.Id == newPollBase.Id);
            await PollBaseCollection.InsertOneAsync(newPollBase);
        }

        public async Task<bool> ExistsAsync(string id)
        {
            return await PollBaseCollection.Find(pollBase => pollBase.Id == id).AnyAsync();
        }

        public async Task<long> StartAsync(string id, DateTime expireDate)
        {
            return (await PollBaseCollection.UpdateOneAsync(pollBase => pollBase.Id == id, 
                Builders<TPoll>.Update
                    .Set(pollBase => pollBase.StartDate, DateTime.Now)
                    .Set(pollBase=> pollBase.ExpireDate,expireDate))).ModifiedCount;
        }

        public async Task<long> StopAsync(string id)
        {
            return (await PollBaseCollection.UpdateOneAsync(pollBase => pollBase.Id == id,Builders<TPoll>.Update.Set(pollBase=>pollBase.ExpireDate,DateTime.Now))).ModifiedCount;
        }

        public virtual async Task<bool> CheckUserCanAnswer(string pollBaseId,string userId)
        {
            var user = await Users.Find(userModel => userModel.UserId == userId).SingleAsync();
            var pollBase = await GetByIdAsync(pollBaseId);
            var answer=await GetAnswersUnitForUserAsync(pollBaseId,userId);
            if (!pollBase.EligibleGroupsNames.Contains(user.GroupName))
                return false;
            if (answer != null)
            {
                if((bool) !pollBase.IsAllowedToReanswer)
                    return false;
            }
            return true;
        }

        public async Task<TAnswersUnit> GetAnswersUnitByIdAsync(string id)
        {
            return await AnswersUnits.Find(answer => answer.Id == id).FirstOrDefaultAsync();
        }

        public async Task<TAnswersUnit> GetAnswersUnitForUserAsync(string pollBaseId,string userId)
        {
            return await AnswersUnits.Find(answer => answer.PollBaseId == pollBaseId&&answer.CreatorUserId==userId).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<TAnswersUnit>> GetPollBaseAnswersAsync(string pollBaseId)
        {
            var pollBase = await GetByIdAsync(pollBaseId);
            return await AnswersUnits.Find(answersUnits=>answersUnits.PollBaseId==pollBaseId).ToListAsync();
        }

        public async Task<IEnumerable<TAnswersUnit>> GetAllAnswersUnitsAsync()
        {
            return await AnswersUnits.Find(_ => true).ToListAsync();
        }

        public virtual async Task AddAnswersUnitAsync(TAnswersUnit answersUnit)
        {
            answersUnit.Id = null;
            answersUnit.AnswerDate=DateTime.Now;
            answersUnit.IsCompleted=answersUnit.CheckCompleted(await GetByIdAsync(answersUnit.PollBaseId));

            await AnswersUnits.InsertOneAsync(answersUnit);
        }

        public virtual async Task UpdateAnswersUnitAsync(TAnswersUnit newAnswersUnit,TAnswersUnit oldAnswersUnit)
        {
            newAnswersUnit.Id = oldAnswersUnit.Id;
            newAnswersUnit.AnswerDate = DateTime.Now;
            newAnswersUnit.CreatorUserId = oldAnswersUnit.CreatorUserId;
            newAnswersUnit.IsCompleted=newAnswersUnit.CheckCompleted(await GetByIdAsync(newAnswersUnit.PollBaseId));

            await AnswersUnits.DeleteOneAsync(answer => answer.Id == newAnswersUnit.Id);
            await AnswersUnits.InsertOneAsync(newAnswersUnit);
        }

        public async Task DeleteAnswersUnitAsync(string id)
        {
            await AnswersUnits.DeleteOneAsync(answer => answer.Id == id);
        }

        public async Task<TAnswersUnit> GetUserPreviousAnswer(string pollBaseId, string userId)
        {
            return await AnswersUnits.Find(answer => answer.CreatorUserId == userId && answer.PollBaseId == pollBaseId).SingleOrDefaultAsync();
        }
    }
}
