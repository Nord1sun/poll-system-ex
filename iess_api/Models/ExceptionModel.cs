using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Runtime.Serialization;

namespace iess_api.Models
{
    public class ExceptionModel
    {
        [BsonId]
        [DataMember]
        [BsonElement("_id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [DataMember]
        [BsonElement("exceptionMessage")]
        public string ExceptionMessage { get; set; }

        [DataMember]
        [BsonElement("timeOfException")]
        public DateTime TimeOfException { get; set; }
    }
}
