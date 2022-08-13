using Discord;
using Discord.Commands;
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

        public EventWalletCommandModule(IMongoRepository<LicenseModel> eventModel,
            IMongoRepository<EventModel> managerModel)
        {
            _licenseModel = eventModel;
            _eventModel = managerModel;
        }

        [Command("balance")]
        public async Task Balance()
        {
            if (await _licenseModel.CheckLicense(Context))
            {
                var guild = Context.Guild;
                if (guild == null) return;

                var eventModel = await _eventModel.FindOneAsync(x => x.DiscordId == guild.Id);
                if (eventModel == null) return;

                if (Context.Channel.Id != eventModel.WalletChannelId) return;

                var userBalance = eventModel.Users.FirstOrDefault(x => x.UserId == Context.User.Id);
                if (userBalance == null)
                {
                    await Context.Message.ReplyAsync("Your account has no balance");
                }
                else
                {
                    await Context.Message.ReplyAsync($"Balance: **{string.Format("{0:#,##0}", userBalance.Amount)}**");
                }
            }
        }
    }
}
