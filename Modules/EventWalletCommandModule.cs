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

            if (Context.Channel.Id != eventModel.WalletChannelId) return;

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
    }
}
