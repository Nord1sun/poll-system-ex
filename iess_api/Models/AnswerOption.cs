using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace iess_api.Models
{
    public class AnswerOption
    {
        [BsonIgnoreIfNull]
        public string QuestionText { get; set; }

        //foreign
        [BsonIgnoreIfNull]
        [BsonRepresentation(BsonType.ObjectId)]
        public string PictureId { get; set; }
    }
}
