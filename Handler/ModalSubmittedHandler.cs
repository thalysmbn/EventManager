using Discord;
using Discord.WebSocket;
using EventManager.Database;
using EventManager.Extensions;
using EventManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventManager.Handler
{
    public class ModalSubmittedHandler
    {
        private readonly DiscordSocketClient _discordSocketClient;
        private readonly IMongoRepository<LicenseModel> _licenseModel;
        private readonly IMongoRepository<EventModel> _eventModel;
        private readonly IMongoRepository<RegionModel> _regionRepository;

        public ModalSubmittedHandler(DiscordSocketClient discordSocketClient,
            IMongoRepository<LicenseModel> eventModel,
            IMongoRepository<EventModel> managerModel,
            IMongoRepository<RegionModel> regionRepository)
        {
            _discordSocketClient = discordSocketClient;
            _licenseModel = eventModel;
            _eventModel = managerModel;
            _regionRepository = regionRepository;
        }

        public async Task Executed(SocketModal modal)
        {
            var language = await _regionRepository.GetOrAddLanguageByRegion(modal.GuildId.Value);
            if (await _licenseModel.CheckLicense(language, modal))
            {
                var eventModel = await _eventModel.FindOneAsync(x => x.DiscordId == modal.GuildId);
                if (eventModel == null) return;
                if (modal.Channel.Id != eventModel.ManagerChannelId) return;

                var customId = modal.Data.CustomId;
                var components = modal.Data.Components.ToList();

                var message = customId.Split("#");

                var command = message[0];
                var messageId = message[1];
                var commandId = message[2];
                var eventId = long.Parse(commandId);

                var eventDataModel = eventModel.Events.SingleOrDefault(x => x.EventId == eventId);
                if (eventDataModel == null) return;

                var component = components.FirstOrDefault(x => x.CustomId == command);

                if (component == null) return;
                if (modal.GuildId == null) return;
                var guild = _discordSocketClient.GetGuild(modal.GuildId.Value);
                var channel = guild.GetVoiceChannel(eventDataModel.VoiceChannelId);

                var eventChannel = _discordSocketClient.GetChannel(eventModel.EventChannelId) as IMessageChannel;
                if (eventChannel == null) return;

                switch (command)
                {
                    case "amount":
                        eventDataModel.Amount = int.Parse(component.Value);
                        break;
                    case "eventTax":
                        var eventTax = components.First(x => x.CustomId == "eventTax").Value;
                        var buyerTax = components.First(x => x.CustomId == "buyerTax").Value;
                        if (eventTax == null || buyerTax == null) break;
                        eventDataModel.EventTax = int.Parse(eventTax);
                        eventDataModel.BuyerTax = int.Parse(buyerTax);
                        break;
                }

                await modal.UpdateAsync(x => x.Embed = new EmbedBuilder().CreateEventBuild(language, eventDataModel));

                if (eventDataModel.MessageId != 0)
                    await eventChannel.ModifyMessageAsync(eventDataModel.MessageId, x =>
                    {
                        x.Embed = new EmbedBuilder().CreatePublicEventBuild(language, eventDataModel);
                        x.Components = new ComponentBuilder().CreatePublicEventComponentBuilder(language, eventDataModel);
                    });

                await _eventModel.ReplaceOneAsync(eventModel);
            }
        }
    }
}
