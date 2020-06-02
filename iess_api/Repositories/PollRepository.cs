using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using iess_api.Interfaces;
using iess_api.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace iess_api.Repositories
{
    public class PollRepository:PollBaseRepository<Poll,PollQuestion,PollAnswersUnit, PollAnswer>,IPollRepository
    {
        public PollRepository(IOptions<MongoDbSettings> settings, IFileRepository pictureRepository, IGroupRepository groupRepository) : base(settings, pictureRepository,groupRepository)
        {
        }

        public class PollAnswerResponse
        {
            public string Group { get; set; }
            public DateTime? AnswerDate { get; set; }
            public IEnumerable<int> SelectedOptions { get; set; }
            public string CreatorUserId { get; set; }
            public string FullName { get; set; }
        }

        public async Task<IEnumerable<IGrouping<string, PollAnswerResponse>>> GetPollQuestionAnswersAsync(string questionId)
        {
            var query = from answersUnit in AnswersUnits.AsQueryable()
                join userModel in Users.AsQueryable() on answersUnit.CreatorUserId equals userModel.UserId
                select new 
                    {
                        FullName = userModel.LastName+" "+userModel.FirstName,
                        userModel.GroupName,
                        answersUnit.AnswerDate,
                        answersUnit.Answers,
                        answersUnit.CreatorUserId
                    };
            var pollAnswerResponses=(await query.ToListAsync()).Where(t=>t.Answers.Any(a=>a.QuestionId==questionId)).Select(t => new PollAnswerResponse()
            {
                FullName=t.FullName,
                AnswerDate = t.AnswerDate,
                CreatorUserId = t.CreatorUserId,
                Group = t.GroupName,
                SelectedOptions = t.Answers.Single(ans=>ans.QuestionId==questionId).SelectedOptions.Select(i=>i+1),
            });
            return pollAnswerResponses.ToLookup(response => response.Group);
        }

        public async Task<Dictionary<int, int>> GetPollQuestionAnswersPlotInfoAsync(string questionId)
        {
            var answers = await AnswersUnits.AsQueryable().SelectMany(unit=>unit.Answers).Where(answer=>answer.QuestionId==questionId).ToListAsync();
            var questionOptionsCount = (await GetPollBaseByQuestionIdAsync(questionId)).Questions.Single(pollQuestion => pollQuestion.Id == questionId).AnswerOptions.Count;
            var dictionary=new Dictionary<int,int>();
            for (int i = 0; i < questionOptionsCount; i++)
            {
                dictionary.Add(i,answers.Count(answer=>answer.SelectedOptions.Any(option=>option==i)));
            }
            return dictionary;
        }
    }

    public interface IPollRepository:IPollBaseRepository<Poll,PollAnswersUnit>
    {
        Task<IEnumerable<IGrouping<string, PollRepository.PollAnswerResponse>>> GetPollQuestionAnswersAsync(string questionId);

        Task<Dictionary<int, int>> GetPollQuestionAnswersPlotInfoAsync(string questionId);
    }
}
