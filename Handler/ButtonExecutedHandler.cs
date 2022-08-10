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
    public class ButtonExecutedHandler
    {
        private readonly DiscordSocketClient _discordSocketClient;
        private readonly IMongoRepository<LicenseModel> _licenseModel;
        private readonly IMongoRepository<EventModel> _eventModel;

        public ButtonExecutedHandler(DiscordSocketClient discordSocketClient,
            IMongoRepository<LicenseModel> eventModel,
            IMongoRepository<EventModel> managerModel)
        {
            _discordSocketClient = discordSocketClient;
            _licenseModel = eventModel;
            _eventModel = managerModel;
        }
        public async Task Executed(SocketMessageComponent component)
        {
            try
            {
                if (await _licenseModel.CheckLicense(component))
                {
                    var eventModel = await _eventModel.FindOneAsync(x => x.DiscordId == component.GuildId);
                    if (eventModel == null) return;

                    var message = component.Data.CustomId.Split("#");

                    var command = message[0];
                    var value = message[1];

                    var eventDataModel = eventModel.Events.SingleOrDefault(x => x.EventId == long.Parse(value));
                    if (eventDataModel == null) return;

                    if (component.GuildId == null) return;
                    var guild = _discordSocketClient.GetGuild(component.GuildId.Value);

                    switch (command)
                    {
                        case "join":
                            if (eventDataModel.IsStopped) break;

                            await Task.Run(async () =>
                            {
                                await component.UpdateAsync(x =>
                                {
                                    x.Components = new ComponentBuilder().CreatePublicEventComponentBuilder(eventDataModel);
                                });
                            }).ContinueWith(async x => { 
                                var queueVoicechannel = guild.GetVoiceChannel(eventModel.QueueVoiceId);
                                if (queueVoicechannel == null) return;

                                var user = guild.GetUser(component.User.Id);
                                if (user == null) return;

                                var userVoiceChannel = user.VoiceChannel;
                                if (userVoiceChannel == null) return;

                                var userModel = eventDataModel.Users.FirstOrDefault(x => x.UserId == user.Id);
                                if (userModel == null)
                                {
                                    eventDataModel.Users.Add(new EventUser
                                    {
                                        UserId = user.Id,
                                        LastUpdate = DateTime.Now,
                                        TimeActivity = 0
                                    });
                                }
                                await _eventModel.ReplaceOneAsync(eventModel);

                                if (userVoiceChannel.Id == queueVoicechannel.Id)
                                    await user.ModifyAsync(x => x.ChannelId = eventDataModel.VoiceChannelId);
                            });
                            break;
                        case "startOrPause":
                        case "stop":
                            if (component.Channel.Id != eventModel.ManagerChannelId) return;
                            var embeds = new LinkedList<Embed>();
                            await Task.Run(async () =>
                            {
                                switch (command)
                                {
                                    case "stop":
                                        if (eventDataModel.IsStopped) break;

                                        var channel = guild.GetVoiceChannel(eventDataModel.VoiceChannelId);
                                        if (channel != null) await channel.DeleteAsync();

                                        eventDataModel.IsStopped = true;
                                        eventDataModel.EndedAt = DateTime.Now;
                                        break;
                                    default:
                                        eventDataModel.IsPaused = !eventDataModel.IsPaused;
                                        break;

                                }
                                embeds.AddFirst(new EmbedBuilder().CreateEventBuild(eventDataModel));
                                await component.UpdateAsync(x =>
                                {
                                    x.Embeds = embeds.ToArray();
                                    x.Components = new ComponentBuilder().CreateEventComponentBuilder(eventDataModel);
                                });
                            }).ContinueWith(async x =>
                            {
                                var eventChannel = _discordSocketClient.GetChannel(eventModel.EventChannelId) as IMessageChannel;
                                if (eventChannel == null) return;
                                if (eventDataModel.MessageId == 0)
                                {
                                    var messageEvent = await eventChannel.SendMessageAsync(" ", embed: new EmbedBuilder().CreatePublicEventBuild(eventDataModel), components: new ComponentBuilder().CreatePublicEventComponentBuilder(eventDataModel));
                                    eventDataModel.MessageId = messageEvent.Id;
                                }
                                else
                                    await eventChannel.ModifyMessageAsync(eventDataModel.MessageId, x =>
                                    {
                                        x.Embed = new EmbedBuilder().CreatePublicEventBuild(eventDataModel);
                                        x.Components = new ComponentBuilder().CreatePublicEventComponentBuilder(eventDataModel);
                                    });
                                await _eventModel.ReplaceOneAsync(eventModel);
                            });
                            break;
                        case "amount":
                            if (component.Channel.Id != eventModel.ManagerChannelId) return;
                            if (eventDataModel.IsStopped) break;
                            await component.RespondWithModalAsync(new ModalBuilder()
                                .WithTitle("Event Amount")
                                .WithCustomId($"amount#{component.Message.Id}#{value}")
                                .AddTextInput("Balance", "amount", placeholder: "Amount", value: eventDataModel.Amount.ToString())
                                .Build());
                            break;
                        case "eventTax":
                            if (component.Channel.Id != eventModel.ManagerChannelId) return;
                            if (eventDataModel.IsStopped) break;
                            await component.RespondWithModalAsync(new ModalBuilder()
                                .WithTitle("Event Tax")
                                .WithCustomId($"eventTax#{component.Message.Id}#{value}")
                                .AddTextInput("Event Tax", "eventTax", placeholder: "Percentage", value: eventDataModel.EventTax.ToString())
                                .AddTextInput("Buyer Tax", "buyerTax", placeholder: "Percentage", value: eventDataModel.BuyerTax.ToString())
                                .Build());
                            break;

                        case "users":
                            if (component.Channel.Id != eventModel.ManagerChannelId) return;

                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
