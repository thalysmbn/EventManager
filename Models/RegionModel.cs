using EventManager.Database;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventManager.Models
{

    [BsonCollection("regions")]
    public class RegionModel : Document
    {
        [BsonElement("discordId")]
        public ulong DiscordId { get; set; }

        [BsonElement("region")]
        public string Region { get; set; }
    }
}
