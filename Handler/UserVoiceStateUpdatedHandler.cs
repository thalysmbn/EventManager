using Discord.WebSocket;
using EventManager.Database;
using EventManager.Models;
using EventManager.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventManager.Handler
{
    public class UserVoiceStateUpdatedHandler
    {
        private readonly IMongoRepository<EventModel> _eventRepository;

        public UserVoiceStateUpdatedHandler(IMongoRepository<EventModel> eventRepository)
        {
            _eventRepository = eventRepository;
        }

        public async Task Executed(SocketUser user, SocketVoiceState before, SocketVoiceState after)
        {
            var guild = GetGuild(before, after);
            if (guild == null) return;

            var eventModel = await _eventRepository.FindOneAsync(x => x.DiscordId == guild.Id);
            if (eventModel == null) return;

            var beforeVoice = before.VoiceChannel;
            var afterVoice = after.VoiceChannel;

            var guildUserModel = eventModel.Users.FirstOrDefault(x => x.UserId == user.Id);

            var beforeChannel = before.VoiceChannel;
            var afterChannel = after.VoiceChannel;
            var disconnected = beforeChannel != null && afterChannel == null;

            if (disconnected)
            {
                if (guildUserModel != null)
                    guildUserModel.CurrentEventId = -1;
            }
            else if (afterChannel != null)
            {
                var eventDataModel = eventModel.Events.SingleOrDefault(x => x.VoiceChannelId == afterChannel.Id);
                if (guildUserModel != null)
                    guildUserModel.CurrentEventId = eventDataModel != null ? eventDataModel.EventId : -1;

            }
            await _eventRepository.ReplaceOneAsync(eventModel);
        }

        private SocketGuild GetGuild(SocketVoiceState x, SocketVoiceState y)
        {
            if (x.VoiceChannel != null) return x.VoiceChannel.Guild;
            if (y.VoiceChannel != null) return y.VoiceChannel.Guild;
            return null;
        }
    }
}
