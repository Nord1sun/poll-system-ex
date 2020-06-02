using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace iess_api.Models
{
    public class RefreshTokenModel
    {
        [BsonId]
        [DataMember]
        [BsonElement("_id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [DataMember]
        [BsonElement("Username")]
        public string Username { get; set; }

        [DataMember]
        [BsonElement("Token")]
        public string Token { get; set; }

        [DataMember]
        [BsonElement("Revoked")]
        public bool Revoked { get; set; }
    }
}
