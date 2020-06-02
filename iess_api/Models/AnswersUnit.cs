using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace iess_api.Models
{
    public class AnswersUnit<TAnswer> where TAnswer:Answer
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        //foreign
        [BsonRepresentation(BsonType.ObjectId)]
        public string PollBaseId { get; set; }

        //foreign
        [BsonRepresentation(BsonType.ObjectId)]
        public string CreatorUserId { get; set; }

        [BsonDateTimeOptions]
        public DateTime? AnswerDate { get; set; }

        public IList<TAnswer> Answers { get; set; }

        public bool? IsCompleted { get; set; }

        public bool CheckCompleted<TQuestion>(PollBase<TQuestion> pollBase) where TQuestion:Question
        {
            return pollBase.Questions.Select(question => question.Id).All(id => Answers.Select(answer => answer.QuestionId).Contains(id));
        }
    }

    public class PollAnswersUnit : AnswersUnit<PollAnswer>
    {

    }

    public class QuizAnswersUnit : AnswersUnit<QuizAnswer>
    {
        public double? TotalAssessment { get; set; }

        public bool? IsChecked { get; set; }

        public int? CurrentReanswerCount { get; set; }
    }
}
