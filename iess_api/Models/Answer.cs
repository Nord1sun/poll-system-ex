using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace iess_api.Models
{
    public abstract class Answer
    {
        //foreign
        [BsonRepresentation(BsonType.ObjectId)]
        public string QuestionId { get; set; }

        [BsonIgnoreIfNull]
        public List<int> SelectedOptions { get; set; }

    }

    public class PollAnswer : Answer
    {

    }

    public class QuizAnswer : Answer
    {
        [BsonIgnoreIfNull]
        public string Text { get; set; }

        public double? Assessment { get; set; }

        public bool? IsChecked { get; set; }
    }
}
