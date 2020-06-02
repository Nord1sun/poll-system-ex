using System;
using MongoDB.Bson;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using MongoDB.Bson.Serialization.Attributes;

namespace iess_api.Models
{
    [DataContract]
    public class UserModel
    {
        [BsonId]
        [DataMember]
        [BsonElement("_id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string UserId { get; set; }

        [DataMember]
        [BsonElement("login")]
        public string Login { get; set; }

        [DataMember]
        [BsonElement("firstname")]
        public string FirstName { get; set; }

        [DataMember]
        [BsonElement("lastname")]
        public string LastName { get; set; }
        
        [DataMember]
        [BsonElement("group")]
        public string GroupName { get; set; }

        [BsonDateTimeOptions]
        [BsonElement("creationdate")]
        public DateTime CreationDate { get; set; }

        [BsonDateTimeOptions]
        [BsonElement("lastlogin")]
        public DateTime LastLogin { get; set; }
        
        [DataMember]
        [BsonElement("passwordhash")]
        public string PasswordHash { get; set; }

        [DataMember]
        [BsonElement("role")]
        public string Role { get; set; }
    }
}
