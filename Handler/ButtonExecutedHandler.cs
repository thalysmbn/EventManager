using Discord;
using Discord.WebSocket;
using EventManager.Database;
using EventManager.Extensions;
using EventManager.Helpers;
using EventManager.Models;
using EventManager.Struct;
using Humanizer;
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
        private readonly IMongoRepository<EventModel> _eventRepository;

        public ButtonExecutedHandler(DiscordSocketClient discordSocketClient,
            IMongoRepository<LicenseModel> eventModel,
            IMongoRepository<EventModel> managerModel)
        {
            _discordSocketClient = discordSocketClient;
            _licenseModel = eventModel;
            _eventRepository = managerModel;
        }
        public async Task Executed(SocketMessageComponent component)
        {
            try
            {
                if (await _licenseModel.CheckLicense(component))
                {
                    var eventModel = await _eventRepository.FindOneAsync(x => x.DiscordId == component.GuildId);
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
                                await _eventRepository.ReplaceOneAsync(eventModel);

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

                                        var amount = eventDataModel.Amount;

                                        var eventTax = ((double)eventDataModel.EventTax / 100) * amount;
                                        var buyerTax = ((double)eventDataModel.BuyerTax / 100) * amount;

                                        var total = (amount - (eventTax + buyerTax));

                                        var users = eventDataModel.Users;
                                        var peakTime = users.Max(x => x.TimeActivity);

                                        var distribution = total / users.Count;

                                        var distributionUsers = new List<UserEventResult>();

                                        var usersByTimeActivity = users.OrderByDescending(x => x.TimeActivity);
                                        var usersTotalToPay = (double)0;
                                        foreach (var user in usersByTimeActivity)
                                        {
                                            var userPecentage = (int)Math.Round((double)(100 * user.TimeActivity) / peakTime);
                                            var userTotalAmount = ((double)userPecentage / 100) * distribution;
                                            usersTotalToPay += userTotalAmount;
                                            distributionUsers.Add(new UserEventResult
                                            {
                                                UserId = user.UserId,
                                                TimeActivity = user.TimeActivity,
                                                Percentage = userPecentage,
                                                Amount = (long)userTotalAmount,
                                            });
                                        }
                                        var rest = total - usersTotalToPay;
                                        var restPrePaid = rest / users.Count;

                                        foreach (var user in distributionUsers)
                                        {
                                            var userData = eventModel.Users.SingleOrDefault(x => x.UserId == user.UserId);
                                            if (userData == null)
                                            {
                                                eventModel.Users.Add(new GuildUser
                                                {
                                                    UserId = user.UserId,
                                                    Amount = user.Amount + (long)restPrePaid
                                                });
                                            }
                                            else
                                            {
                                                userData.Amount += user.Amount + (long)restPrePaid;
                                            }
                                        }
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
                                await _eventRepository.ReplaceOneAsync(eventModel);
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
                            var embed = new EmbedBuilder();
                            var amount = eventDataModel.Amount;

                            var eventTax = ((double)eventDataModel.EventTax / 100) * amount;
                            var buyerTax = ((double)eventDataModel.BuyerTax / 100) * amount;

                            var total = (amount - (eventTax + buyerTax));

                            var users = eventDataModel.Users;
                            var peakTime = users.Max(x => x.TimeActivity);

                            embed.AddField("Amount:", string.Format("{0:#,##0}", eventDataModel.Amount));
                            embed.AddField("Total:", string.Format("{0:#,##0}", total));
                            embed.AddField("Users:", eventDataModel.Users.Count);
                            embed.AddField("Peak Time:", TimeSpan.FromSeconds(peakTime).Humanize(3));
                            embed.AddField("Total Time:", TimeSpan.FromSeconds(eventDataModel.TotalEventTime).Humanize(3));
                            embed.AddField("Event Tax:", string.Format("{0:#,##0}", eventTax), true);
                            embed.AddField("Buyer Tax:", string.Format("{0:#,##0}", buyerTax), true);

                            var stringBuilder = new StringBuilder();
                            stringBuilder.AppendLine("Details:");

                            var distribution = total / users.Count;

                            var distributionUsers = new List<UserEventResult>();

                            var usersByTimeActivity = users.OrderByDescending(x => x.TimeActivity);
                            var usersTotalToPay = (double)0;
                            foreach (var user in usersByTimeActivity)
                            {
                                var userPecentage = (int)Math.Round((double)(100 * user.TimeActivity) / peakTime);
                                var userTotalAmount = ((double)userPecentage / 100) * distribution;
                                usersTotalToPay += userTotalAmount;
                                distributionUsers.Add(new UserEventResult
                                {
                                    UserId = user.UserId,
                                    TimeActivity = user.TimeActivity,
                                    Percentage = userPecentage,
                                    Amount = (long)userTotalAmount,
                                });
                            }
                            var rest = total - usersTotalToPay;
                            var restPrePaid = rest / users.Count;

                            foreach (var user in distributionUsers)
                            {
                                stringBuilder.Append($"<@{user.UserId}>");
                                stringBuilder.Append($" **{string.Format("{0:#,##0}", user.Amount + restPrePaid)}** ");
                                stringBuilder.Append($" ``{TimeSpan.FromSeconds(user.TimeActivity).Humanize(3)}`` ");
                                stringBuilder.Append($" ( {user.Percentage}% ) ");
                                stringBuilder.AppendLine();
                            }

                            await component.RespondAsync(stringBuilder.ToString(),
                            embed: embed.Build()
                        );
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
