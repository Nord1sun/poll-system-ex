using System;
using System.Linq;
using MongoDB.Bson;
using System.Threading.Tasks;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace iess_api.Models
{
    public class GroupModel
    {
        [BsonId]
        [BsonElement("_id")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        
        [BsonElement("Name")]
        public string Name { get; set; }
    }
}
