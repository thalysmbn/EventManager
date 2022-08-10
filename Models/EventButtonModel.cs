using EventManager.Database;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventManager.Models
{
    [BsonCollection("events_buttons")]
    public class EventButtonModel : Document
    {
        [BsonElement("discordId")]
        public ulong DiscordId { get; set; }

        [BsonElement("eventId")]
        public long EventId { get; set; }

        [BsonElement("eventModelId")]
        public ObjectId EventModelId { get; set; }
    }
}
