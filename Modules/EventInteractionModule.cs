using Discord;
using Discord.Interactions;
using EventManager.Database;
using EventManager.Extensions;
using EventManager.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventManager.Modules
{
    public class EventInteractionModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly IMongoRepository<LicenseModel> _licenseModel;
        private readonly IMongoRepository<EventModel> _eventModel;
        private readonly IMongoRepository<RegionModel> _regionRepository;

        public EventInteractionModule(IMongoRepository<LicenseModel> eventModel,
            IMongoRepository<EventModel> managerModel,
            IMongoRepository<RegionModel> regionRepository)
        {
            _licenseModel = eventModel;
            _eventModel = managerModel;
            _regionRepository = regionRepository;
        }

        [SlashCommand("build", "Build Event System")]
        [RequireRole("Manager")]
        public async Task Build()
        {
            var guild = Context.Guild;
            var eventModel = await _eventModel.FindOneAsync(x => x.DiscordId == Context.Guild.Id);
            if (eventModel == null)
            {
                var categoryChannel = await guild.CreateCategoryChannelAsync("Event Manager");

                var categoryVoiceChannel = await guild.CreateCategoryChannelAsync("Event Voice");
                var queueVoiceChannel = await guild.CreateVoiceChannelAsync("Queue Event", x => x.CategoryId = categoryVoiceChannel.Id);

                var managerChannel = await guild.CreateTextChannelAsync("manager", x => x.CategoryId = categoryChannel.Id);
                var eventChannel = await guild.CreateTextChannelAsync("events", x => x.CategoryId = categoryChannel.Id);
                var walletChannel = await guild.CreateTextChannelAsync("wallet", x => x.CategoryId = categoryChannel.Id);
                var logChannel = await guild.CreateTextChannelAsync("logs", x => x.CategoryId = categoryChannel.Id);

                await _eventModel.InsertOneAsync(new EventModel
                {
                    DiscordId = Context.Guild.Id,
                    CategoryId = categoryChannel.Id,
                    CategoryVoiceId = categoryVoiceChannel.Id,
                    QueueVoiceId = queueVoiceChannel.Id,
                    ManagerChannelId = managerChannel.Id,
                    EventChannelId = eventChannel.Id,
                    WalletChannelId = walletChannel.Id,
                    LogChannelId = logChannel.Id,
                    Events = new List<Event>(),
                    Users = new List<GuildUser>()
                });

                await RespondAsync("Event Manager builded.");
            }
            else
            {
                if (!guild.CategoryChannels.Any(x => x.Id == eventModel.CategoryId))
                {
                    var categoryChannel = await guild.CreateCategoryChannelAsync("Event Manager");
                    eventModel.CategoryId = categoryChannel.Id;
                }
                if (!guild.CategoryChannels.Any(x => x.Id == eventModel.CategoryVoiceId))
                {
                    var categoryChannel = await guild.CreateCategoryChannelAsync("Event Voice");
                    var queueVoiceChannel = await guild.CreateVoiceChannelAsync("Queue Event", x => x.CategoryId = categoryChannel.Id);
                    eventModel.CategoryVoiceId = categoryChannel.Id;
                    eventModel.QueueVoiceId = queueVoiceChannel.Id;
                }
                if (!guild.Channels.Any(x => x.Id == eventModel.ManagerChannelId))
                {
                    var managerChannel = await guild.CreateTextChannelAsync("manager", x => x.CategoryId = eventModel.CategoryId);
                    eventModel.ManagerChannelId = managerChannel.Id;
                }
                if (!guild.Channels.Any(x => x.Id == eventModel.EventChannelId))
                {
                    var eventChannel = await guild.CreateTextChannelAsync("events", x => x.CategoryId = eventModel.CategoryId);
                    eventModel.EventChannelId = eventChannel.Id;
                }
                if (!guild.Channels.Any(x => x.Id == eventModel.WalletChannelId))
                {
                    var walletChannel = await guild.CreateTextChannelAsync("wallet", x => x.CategoryId = eventModel.CategoryId);
                    eventModel.WalletChannelId = walletChannel.Id;
                }
                if (!guild.Channels.Any(x => x.Id == eventModel.WalletChannelId))
                {
                    var logChannel = await guild.CreateTextChannelAsync("logs", x => x.CategoryId = eventModel.CategoryId);
                    eventModel.LogChannelId = logChannel.Id;
                }
                await _eventModel.ReplaceOneAsync(eventModel);

                await RespondAsync("Event Manager sync.");
            }
        }

        [SlashCommand("create", "Create Event")]
        [RequireRole("Manager")]
        public async Task Create(int eventTax, int buyerTax)
        {
            var language = await _regionRepository.GetOrAddLanguageByRegion(Context.Guild.Id);
            if (await _licenseModel.CheckLicense(language, Context))
            {
                var guild = Context.Guild;
                if (guild == null) return;

                var eventModel = await _eventModel.FindOneAsync(x => x.DiscordId == guild.Id);
                if (eventModel == null) return;
                if (Context.Channel.Id != eventModel.ManagerChannelId) return;

                var eventId = eventModel.LastEventId++;

                var eventChannel = guild.GetChannel(eventModel.EventChannelId) as IMessageChannel;
                if (eventChannel == null) return;

                var eventCategoryVoiceChannel = guild.GetChannel(eventModel.CategoryVoiceId);
                if (eventCategoryVoiceChannel == null) return;

                var voiceChannel = await guild.CreateVoiceChannelAsync($"Event #{eventId}", x => {
                    x.CategoryId = eventModel.CategoryVoiceId;
                });

                var objectId = ObjectId.GenerateNewId();
                var dateNow = DateTime.Now;

                var eventDataModel = new Event
                {
                    Id = objectId,
                    EventId = eventId,
                    VoiceChannelId = voiceChannel.Id,
                    MessageId = 0,
                    Manager = Context.User.Id,
                    IsPaused = true,
                    IsStopped = false,
                    Amount = 0,
                    EventTax = eventTax,
                    BuyerTax = buyerTax,
                    CreatedAt = dateNow,
                    LastStart = dateNow,
                    EndedAt = dateNow,
                    TotalEventTime = 0,
                    Users = new List<EventUser>()
                };
                
                eventModel.Events.Add(eventDataModel);

                await Context.Interaction.RespondAsync($"ID: {eventDataModel.Id}",
                    embeds: new[] { new EmbedBuilder().CreateEventBuild(language, eventDataModel) },
                    components: new ComponentBuilder().CreateEventComponentBuilder(language, eventDataModel));
                
                await _eventModel.ReplaceOneAsync(eventModel);
            }
        }
    }
}
