using Discord.WebSocket;
using EventManager.Database;
using EventManager.Models;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace EventManager.Services
{
    public class UserActivity : IHostedService
    {
        private readonly DiscordSocketClient _discordSocketClient;
        private readonly IMongoRepository<EventModel> _eventRepository;
        private System.Timers.Timer _timer { get; set; }

        public UserActivity(DiscordSocketClient discordSocketClient,
            IMongoRepository<EventModel> eventRepository)
        {
            _discordSocketClient = discordSocketClient;
            _eventRepository = eventRepository;
            _timer = new System.Timers.Timer();
            _timer.Elapsed += Execute;
            _timer.Interval = 5000;
            _timer.Enabled = true;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer.Start();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer.Stop();

            return Task.CompletedTask;
        }

        private void Execute(object source, ElapsedEventArgs e)
        {
            var lastUpdate = DateTime.Now;
            var eventModels = _eventRepository.AsQueryable().Where(x => x.Events.Any(x => !x.IsPaused && !x.IsStopped));
            foreach (var eventModel in eventModels)
            {
                var guild = _discordSocketClient.GetGuild(eventModel.DiscordId);
                if (guild == null) continue;

                foreach (var eventsModel in eventModel.Events)
                {
                    var voiceChannel = guild.VoiceChannels.FirstOrDefault(x => x.Id == eventsModel.VoiceChannelId);
                    if (voiceChannel == null) continue;

                    eventsModel.TotalEventTime += 5;

                    foreach (var user in eventsModel.Users)
                    {
                        if (voiceChannel.ConnectedUsers.Any(x => x.Id == user.UserId))
                            user.TimeActivity += 5;
                    }
                }
                _eventRepository.ReplaceOne(eventModel);
            }
        }
    }
}
