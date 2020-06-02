using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using iess_api.Constants;
using iess_api.Interfaces;
using iess_api.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace iess_api.Repositories
{
    public class BriefQuizAnswerResponse
    {
        public string FullName { get; set; }
        public string GroupName { get; set; }
        public DateTime? AnswerDate { get; set; }
        public string CreatorUserId { get; set; }
        public string Id { get; set; }
        public double? TotalAssessment { get; set; }
        public bool? IsChecked { get; set; }
    }

    public class BriefQuizResponse:BriefPollBaseResponse
    {
        //public int? MaxReanswerCount { get; set; }

        //public int? CurrentReanswerCount { get; set; }
    }

    public class QuizRepository:PollBaseRepository<Quiz,QuizQuestion,QuizAnswersUnit, QuizAnswer>,IQuizRepository
    {
        public QuizRepository(IOptions<MongoDbSettings> settings, IFileRepository pictureRepository, IGroupRepository groupRepository) : base(settings, pictureRepository,groupRepository)
        {
        }
        /// <summary>
        /// Returns Quiz without <see cref="QuizQuestion.CorrectAnswerOptions"/>
        /// </summary>
        public override async Task<Quiz> GetByIdAsync(string id)
        { 
            return await PollBaseCollection.Find(quiz => quiz.Id == id).Project(Builders<Quiz>.Projection.Expression(quiz=>Quiz.ExcludeCorrectAnswers(quiz))).FirstOrDefaultAsync();
        }

        /*public override async Task<List<BriefPollBaseResponse>> GetAllAvailableForUserAsync(string userId)
        {
            var student = await Users.Find(user => user.UserId == userId).SingleAsync();
            var query = PollBaseCollection.AsQueryable()
                .Where(quiz=>quiz.EligibleGroupsNames.Any(group=>group==student.GroupName)&&quiz.StartDate<DateTime.Now&&quiz.ExpireDate>DateTime.Now)
                .Join(Users.AsQueryable(), quiz => quiz.CreatorUserId, creatorUserModel => creatorUserModel.UserId,
                    (quiz, creatorUserModel) => new BriefQuizResponse
                    {
                        Id = quiz.Id,
                        Title = quiz.Title,
                        StartDate = quiz.StartDate,
                        ExpireDate = quiz.ExpireDate,
                        EligibleGroups = quiz.EligibleGroupsNames,
                        FirstName = creatorUserModel.FirstName,
                        LastName = creatorUserModel.LastName,
                        CreatorUserId = quiz.CreatorUserId,
                        IsAllowedToReanswer=quiz.IsAllowedToReanswer,
                        AreStatsPublic=quiz.AreStatsPublic,
                        //MaxReanswerCount = quiz.MaxReanswerCount,
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
                    //if (answersUnit.CurrentReanswerCount >= response.MaxReanswerCount)
                    //    continue;
                    //response.CurrentReanswerCount = answersUnit.CurrentReanswerCount;
                }

                response.HasAnswer = answersUnit!=null;
                resultList.Add(response);
            }
            return resultList;
        }*/
        
        public override async Task AddAsync(Quiz quiz)
        {
            quiz.Id = null;
            quiz.CreationDate = DateTime.Now;
            quiz.MaxAssessment = quiz.Questions.Sum(question=>question.MaxAssessment);
            quiz.MaxReanswerCount = int.MaxValue;

            foreach (var question in quiz.Questions)
            {
                if (Types.QuizAnswerTypes[question.AnswerType] != QuizAnswerType.TextInput)
                {
                    question.CorrectAnswerOptions=question.CorrectAnswerOptions.OrderBy(i => i).ToList();
                }
            }
            await PollBaseCollection.InsertOneAsync(quiz);
        }

        public override async Task UpdateAsync(Quiz newQuiz)
        {
            var oldQuiz = await GetByIdAsync(newQuiz.Id);

            newQuiz.Id = oldQuiz.Id;
            newQuiz.CreationDate = oldQuiz.CreationDate;
            newQuiz.CreatorUserId = oldQuiz.CreatorUserId;

            newQuiz.MaxAssessment = newQuiz.Questions.Sum(question=>question.MaxAssessment);
            foreach (var question in newQuiz.Questions)
            {
                if (Types.QuizAnswerTypes[question.AnswerType] != QuizAnswerType.TextInput)
                {
                    question.CorrectAnswerOptions=question.CorrectAnswerOptions.OrderBy(i => i).ToList();
                }
            }

            await PollBaseCollection.DeleteOneAsync(poll => poll.Id == newQuiz.Id);
            await PollBaseCollection.InsertOneAsync(newQuiz);
        }

        public override async Task<bool> CheckUserCanAnswer(string pollBaseId,string userId)
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
                if(answer.CurrentReanswerCount>=pollBase.MaxReanswerCount)
                    return false;
            }
            return true;
        }

        public override async Task AddAnswersUnitAsync(QuizAnswersUnit answersUnit)
        {
            answersUnit.Id = null;
            answersUnit.AnswerDate=DateTime.Now;
            answersUnit.CurrentReanswerCount = 0;

            answersUnit.IsCompleted=answersUnit.CheckCompleted(await GetByIdAsync(answersUnit.PollBaseId));
            await AssessAnswersUnitAsync(answersUnit);

            await AnswersUnits.InsertOneAsync(answersUnit);
        }

        public override async Task UpdateAnswersUnitAsync(QuizAnswersUnit newAnswersUnit,QuizAnswersUnit oldAnswersUnit)
        {
            newAnswersUnit.Id = oldAnswersUnit.Id;
            newAnswersUnit.AnswerDate = DateTime.Now;
            newAnswersUnit.CreatorUserId = oldAnswersUnit.CreatorUserId;
            newAnswersUnit.CurrentReanswerCount = oldAnswersUnit.CurrentReanswerCount + 1;

            newAnswersUnit.IsCompleted=newAnswersUnit.CheckCompleted(await GetByIdAsync(newAnswersUnit.PollBaseId));
            await AssessAnswersUnitAsync(newAnswersUnit);

            await AnswersUnits.DeleteOneAsync(answer => answer.Id == newAnswersUnit.Id);
            await AnswersUnits.InsertOneAsync(newAnswersUnit);
        }

        private async Task AssessAnswersUnitAsync(QuizAnswersUnit answersUnit)
        {
            var quiz=await base.GetByIdAsync(answersUnit.PollBaseId);
            foreach (var answer in answersUnit.Answers)
            {
                var quizQuestion = quiz.Questions.SingleOrDefault(question =>question.Id == answer.QuestionId);
                if (quizQuestion==null)
                    continue;
                if (Types.QuizAnswerTypes[quizQuestion.AnswerType] == QuizAnswerType.TextInput)
                {
                    answer.IsChecked = false;
                }
                else
                {
                    answer.IsChecked = true;
                    if ((bool) quizQuestion.PreciseMatch)
                    {
                        answer.Assessment= quizQuestion.CorrectAnswerOptions.SequenceEqual(answer.SelectedOptions.OrderBy(i => i))?quizQuestion.MaxAssessment:0;
                    }
                    else
                    {
                        if (answer.SelectedOptions.Count > quizQuestion.CorrectAnswerOptions.Count)
                            answer.Assessment = 0;
                        else
                            answer.Assessment = quizQuestion.CorrectAnswerOptions.Intersect(answer.SelectedOptions).Count() /
                                            quizQuestion.CorrectAnswerOptions.Count * quizQuestion.MaxAssessment;
                    }
                }
            }
            answersUnit.TotalAssessment = answersUnit.Answers.Sum(answer => answer.Assessment);
            answersUnit.IsChecked=answersUnit.Answers.All(answer => answer.IsChecked??false);
        }

        public async Task<Quiz> GetQuizCorrectAnswerOptionsAsync(string id)
        {
            return await PollBaseCollection.Find(quiz => quiz.Id == id).Project(Builders<Quiz>.Projection.Expression(quiz => Quiz.CorrectAnswersOnly(quiz))).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<IGrouping<string,BriefQuizAnswerResponse>>> GetQuizAnswersAsync(string quizId)
        {
            var query = from answersUnit in AnswersUnits.AsQueryable().Where(unit=>unit.PollBaseId==quizId)
                join userModel in Users.AsQueryable() on answersUnit.CreatorUserId equals userModel.UserId
                select new BriefQuizAnswerResponse
                {
                    Id=answersUnit.Id,
                    FullName = userModel.LastName + " " + userModel.FirstName,
                    GroupName = userModel.GroupName,
                    AnswerDate = answersUnit.AnswerDate,
                    CreatorUserId = answersUnit.CreatorUserId,
                    TotalAssessment=answersUnit.TotalAssessment,
                    IsChecked=answersUnit.IsChecked
                };
            var responses = await query.ToListAsync();
            return responses.ToLookup(r => r.GroupName);
        }

        public async Task<long> AssessTextAnswerAsync(AssessTextAnswerModel assessModel)
        {
            var answersUnit = await AnswersUnits.Find(unit => unit.Id == assessModel.AnswerUnitId).FirstOrDefaultAsync();
            var newAssessment =  answersUnit.TotalAssessment + assessModel.Assessment;
            answersUnit.Answers.Single(a=>a.QuestionId==assessModel.QuestionId).IsChecked=true;
            var unitChecked = answersUnit.Answers.All(a => (bool) a.IsChecked);
            return (await AnswersUnits.UpdateOneAsync(unit => unit.Id == assessModel.AnswerUnitId&&unit.Answers.Any(answer=>answer.QuestionId==assessModel.QuestionId), 
                Builders<QuizAnswersUnit>.Update
                .Set(quiz => quiz.Answers[-1].Assessment, assessModel.Assessment)
                .Set(quiz => quiz.Answers[-1].IsChecked, true)
                .Set(quiz => quiz.TotalAssessment, newAssessment)
                .Set(quiz => quiz.IsChecked, unitChecked)
            )).ModifiedCount;
        }
    }

    public interface IQuizRepository:IPollBaseRepository<Quiz,QuizAnswersUnit>
    {
        Task<Quiz> GetQuizCorrectAnswerOptionsAsync(string id);

        Task<IEnumerable<IGrouping<string, BriefQuizAnswerResponse>>> GetQuizAnswersAsync(string quizId);

        Task<long> AssessTextAnswerAsync(AssessTextAnswerModel assessModel);
    }
}
