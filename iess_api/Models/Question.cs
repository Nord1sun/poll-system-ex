using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace iess_api.Models
{
    public abstract class Question
    {
        protected Question()
        {
            Id = ObjectId.GenerateNewId().ToString();
        }

        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string AnswerType { get; set; }

        public string QuestionText { get; set; }

        //foreign
        [BsonIgnoreIfNull]
        [BsonRepresentation(BsonType.ObjectId)]
        public string PictureId { get; set; }

        [BsonIgnoreIfNull]
        public IList<AnswerOption> AnswerOptions { get; set; }
        
    }

    public class PollQuestion : Question
    {

    }

    public class QuizQuestion : Question
    {
        [BsonIgnoreIfNull]
        public IList<int> CorrectAnswerOptions { get; set; }

        public double? MaxAssessment { get; set; }

        public bool? PreciseMatch { get; set; }
    }
}
