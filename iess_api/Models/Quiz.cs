using System.Collections.Generic;
using iess_api.Constants;

namespace iess_api.Models
{
    public class Quiz:PollBase<QuizQuestion>
    {
        public double? MaxAssessment { get; set; }

        public int? MaxReanswerCount { get; set; }

        public static Quiz ExcludeCorrectAnswers(Quiz quiz)
        {
            foreach (var question in quiz.Questions)
            {
                question.CorrectAnswerOptions = null;
            }
            return quiz;
        }

        public static Quiz CorrectAnswersOnly(Quiz quiz)
        {
            var newPoll = new Quiz
            {
                Id = quiz.Id,
                Questions = new List<QuizQuestion>()
            };
            foreach (var question in quiz.Questions)
            {
                if(Types.QuizAnswerTypes[question.AnswerType] == QuizAnswerType.TextInput)
                    continue;
                newPoll.Questions.Add(new QuizQuestion
                {
                    Id=question.Id,
                    CorrectAnswerOptions = question.CorrectAnswerOptions
                });
            }
            return newPoll;
        }
    }
}
