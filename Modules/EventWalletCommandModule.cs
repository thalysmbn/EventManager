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
            var license = _licenseModel.FindOneAsync(x => x.DiscordId != Context.Guild.Id);
            await Context.Message.ReplyAsync("Pong!");
        }
    }
}
