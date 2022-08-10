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
    [BsonCollection("events")]
    public class EventModel : Document
    {
        [BsonElement("discordId")]
        public ulong DiscordId { get; set; }

        [BsonElement("lastEventId")]
        public long LastEventId { get; set; }

        [BsonElement("queueVoiceId")]
        public ulong QueueVoiceId { get; set; }

        [BsonElement("categoryId")]
        public ulong CategoryId { get; set; }

        [BsonElement("categoryVoiceId")]
        public ulong CategoryVoiceId { get; set; }

        [BsonElement("managerChannelId")]
        public ulong ManagerChannelId { get; set; }

        [BsonElement("eventChannelId")]
        public ulong EventChannelId { get; set; }

        [BsonElement("walletChannelId")]
        public ulong WalletChannelId { get; set; }

        [BsonElement("logChannelId")]
        public ulong LogChannelId { get; set; }

        public IList<GuildUser> Users { get; set; }

        public IList<Event> Events { get; set; }
    }

    public class GuildUser
    {
        [BsonElement("userId")]
        public ulong UserId { get; set; }

        [BsonElement("currentEventId")]
        public long CurrentEventId { get; set; }

        [BsonElement("currentVoiceChannelId")]
        public ulong CurrentVoiceChannelId { get; set; }

        [BsonElement("amount")]
        public int Amount { get; set; }
    }

    public class Event
    {
        [BsonElement("_id")]
        public ObjectId Id { get; set; }

        [BsonElement("eventId")]
        public long EventId { get; set; }

        [BsonElement("messageId")]
        public ulong MessageId { get; set; }

        [BsonElement("voiceChannelId")]
        public ulong VoiceChannelId { get; set; }

        [BsonElement("manager")]
        public ulong Manager { get; set; }

        [BsonElement("amount")]
        public int Amount { get; set; }

        [BsonElement("eventTax")]
        public int EventTax { get; set; }

        [BsonElement("buyerTax")]
        public int BuyerTax { get; set; }

        [BsonElement("isPaused")]
        public bool IsPaused { get; set; }

        [BsonElement("isStopped")]
        public bool IsStopped { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        [BsonElement("lastStart")]
        public DateTime LastStart { get; set; }

        [BsonElement("endedAt")]
        public DateTime EndedAt { get; set; }

        [BsonElement("totalEventTime")]
        public long TotalEventTime { get; set; }

        [BsonElement("users")]
        public IList<EventUser> Users { get; set; }
    }

    public class EventUser
    {
        [BsonElement("userId")]
        public ulong UserId { get; set; }

        [BsonElement("lastUpdate")]
        public DateTime LastUpdate { get; set; }

        [BsonElement("timeActivity")]
        public long TimeActivity { get; set; }
    }
}
