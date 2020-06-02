using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace iess_api.Models
{
    public abstract class PollBase< TQuestion> where TQuestion:Question
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        //foreign
        [BsonRepresentation(BsonType.ObjectId)]
        public string CreatorUserId { get; set; }

        [BsonDateTimeOptions]
        public DateTime? CreationDate { get; set; }

        [BsonDateTimeOptions]
        [BsonIgnoreIfNull]
        public DateTime? StartDate { get; set; }

        [BsonDateTimeOptions]
        [BsonIgnoreIfNull]
        public DateTime? ExpireDate { get; set; }

        public string Title { get; set; }

        public IList<string> EligibleGroupsNames { get; set; }

        public IList<TQuestion> Questions { get; set; }

        public bool? IsAllowedToReanswer { get; set; }

        public bool? AreStatsPublic { get; set; }
        
        public static List<string> GetReplacedPicturesList(PollBase< TQuestion> oldPollBase,PollBase< TQuestion> newPollBase)
        {
            var replacedPictures=new List<string>();
            foreach (var oldQuestion in oldPollBase.Questions)
            {
                var newQuestion = newPollBase.Questions.SingleOrDefault(question => question.Id == oldQuestion.Id);
                if(newQuestion==null)
                    continue;
                if (newQuestion.PictureId!=oldQuestion.PictureId)
                    replacedPictures.Add(oldQuestion.PictureId);
                replacedPictures.AddRange(oldQuestion.AnswerOptions.Select(oldOption => oldOption.PictureId).Where(oldOption =>
                    !newQuestion.AnswerOptions.Select(newOption => newOption.PictureId).Contains(oldOption)));
            }
            return replacedPictures;
        }
    }

    public class Poll : PollBase<PollQuestion>
    {
        public bool? AreAnswersAnonymous { get; set; }
    }
}
