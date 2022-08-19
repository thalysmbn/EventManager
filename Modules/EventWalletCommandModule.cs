using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using EventManager.Database;
using EventManager.Extensions;
using EventManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventManager.Modules
{
    [Name("Wallet")]
    public class EventWalletCommandModule : ModuleBase<SocketCommandContext>
    {
        private readonly IMongoRepository<LicenseModel> _licenseModel;
        private readonly IMongoRepository<EventModel> _eventModel;
        private readonly IMongoRepository<RegionModel> _regionRepository;

        public EventWalletCommandModule(IMongoRepository<LicenseModel> eventModel,
            IMongoRepository<EventModel> managerModel,
            IMongoRepository<RegionModel> regionRepository)
        {
            _licenseModel = eventModel;
            _eventModel = managerModel;
            _regionRepository = regionRepository;
        }

        [Command("remove")]
        [Alias("remover")]
        public async Task Remove(SocketGuildUser user, long amount)
        {
            if (user == null) return;

            var language = await _regionRepository.GetOrAddLanguageByRegion(Context.Guild.Id);
            if (!await _licenseModel.CheckLicense(language, Context)) return;

            var guild = Context.Guild;
            if (guild == null) return;

            var _user = guild.GetUser(Context.User.Id);
            if (_user == null) return;

            if (!_user.Roles.Any(x => x.Name == "Manager")) return;

            var eventModel = await _eventModel.FindOneAsync(x => x.DiscordId == guild.Id);
            if (eventModel == null) return;

            if (Context.Channel.Id != eventModel.WalletChannelId) return;

            var userBalance = eventModel.Users.FirstOrDefault(x => x.UserId == user.Id);
            if (userBalance == null)
            {
                await Context.Message.ReplyAsync($"{language.AccountWithoutBalance}");
            }
            else
            {
                var remove = userBalance.Amount - amount;
                if (remove < 0) remove = 0;
                userBalance.Amount = remove;
                await _eventModel.ReplaceOneAsync(eventModel);
                await Context.Message.ReplyAsync($"{language.AccountAmountUpdated} ``-{amount}`` ( **{string.Format("{0:#,##0}", userBalance.Amount)}** )");
            }
        }

        [Command("add")]
        [Alias("adicionar")]
        public async Task Add(IGuildUser user, long amount)
        {
            var guild = Context.Guild;
            if (guild == null)
            {
                await Context.Message.ReplyAsync($"G404");
                return;
            }

            if (user == null)
            {
                await Context.Message.ReplyAsync($"U404");
                return;
            }

            var language = await _regionRepository.GetOrAddLanguageByRegion(Context.Guild.Id);
            if (!await _licenseModel.CheckLicense(language, Context)) return;

            var _user = guild.GetUser(Context.User.Id);
            if (_user == null) return;

            if (!_user.Roles.Any(x => x.Name == "Manager"))
            {
                await Context.Message.ReplyAsync($"U403");
                return;
            }

            var eventModel = await _eventModel.FindOneAsync(x => x.DiscordId == guild.Id);
            if (eventModel == null)
            {
                await Context.Message.ReplyAsync($"E403");
                return;
            }

            if (Context.Channel.Id != eventModel.WalletChannelId)
            {
                await Context.Message.ReplyAsync($"C403");
                return;
            }

            var userBalance = eventModel.Users.FirstOrDefault(x => x.UserId == user.Id);
            if (userBalance == null)
            {
                userBalance = new GuildUser
                {
                    UserId = user.Id,
                    CurrentEventId = -1,
                    Amount = amount
                };
                eventModel.Users.Add(userBalance);
            }
            else
            {
                var remove = userBalance.Amount + amount;
                if (remove < 0) remove = 0;
                userBalance.Amount = remove;
            }
            await _eventModel.ReplaceOneAsync(eventModel);
            await Context.Message.ReplyAsync($"{language.AccountAmountUpdated} ``+{amount}`` ( **{string.Format("{0:#,##0}", userBalance.Amount)}** )");
        }

        [Command("balance")]
        [Alias("saldo")]
        public async Task Balance()
        {
            var language = await _regionRepository.GetOrAddLanguageByRegion(Context.Guild.Id);
            if (!await _licenseModel.CheckLicense(language, Context)) return;

            var guild = Context.Guild;
            if (guild == null) return;

            var eventModel = await _eventModel.FindOneAsync(x => x.DiscordId == guild.Id);
            if (eventModel == null) return;

            if (Context.Channel.Id == eventModel.WalletChannelId)
            {

                var userBalance = eventModel.Users.FirstOrDefault(x => x.UserId == Context.User.Id);
                if (userBalance == null)
                {
                    await Context.Message.ReplyAsync($"{language.AccountWithoutBalance}");
                }
                else
                {
                    await Context.Message.ReplyAsync($"{language.Balance}: **{string.Format("{0:#,##0}", userBalance.Amount)}**");
                }
            }
            else if (Context.Channel.Id == eventModel.ManagerChannelId)
            {
                var totalBalance = eventModel.Users.Sum(x => x.Amount);
                await Context.Message.ReplyAsync($"{language.Balance}: **{string.Format("{0:#,##0}", totalBalance)}**\n{language.Users}: **{eventModel.Users.Count}**");
            }
        }

        [Command("wallets")]
        [Alias("carteiras")]
        public async Task Wallets()
        {
            var language = await _regionRepository.GetOrAddLanguageByRegion(Context.Guild.Id);
            if (!await _licenseModel.CheckLicense(language, Context)) return;

            var guild = Context.Guild;
            if (guild == null) return;

            var eventModel = await _eventModel.FindOneAsync(x => x.DiscordId == guild.Id);
            if (eventModel == null) return;

            if (Context.Channel.Id == eventModel.ManagerChannelId)
            {

                var itemPerPage = 10;
                var pageMath = eventModel.Users.Count / itemPerPage;
                var pageNum = pageMath == 0 ? 1 : pageMath;

                var userList = eventModel.Users.OrderByDescending(x => x.Amount);
                for (int i = 1; i <= pageNum; i++)
                {
                    var embed = new EmbedBuilder();

                    var stringBuilderId = new StringBuilder("");
                    var stringBuilderUser = new StringBuilder("");
                    var stringBuilderAmount = new StringBuilder("");

                    var result = userList.Skip((i - 1) * itemPerPage).Take(itemPerPage);

                    foreach (var user in result)
                    {
                        stringBuilderId.AppendLine(user.UserId.ToString());
                        stringBuilderUser.AppendLine($"<@{user.UserId}>");
                        stringBuilderAmount.AppendLine(string.Format("{0:#,##0}", user.Amount));
                    }

                    embed.AddField($"ID:", stringBuilderId.ToString(), true)
                            .AddField($"{language.User}:", stringBuilderUser.ToString(), true)
                            .AddField($"{language.Amount}:", stringBuilderAmount.ToString(), true);

                    var message = await Context.Channel.SendMessageAsync($" ", embed: embed.Build());
                }
            }
        }
    }
}
