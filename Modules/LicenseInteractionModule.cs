using Discord;
using Discord.Interactions;
using EventManager.Database;
using EventManager.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventManager.Modules
{
    public class LicenseInteractionModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly IMongoRepository<LicenseModel> _licenseModel;
        private readonly IMongoRepository<EventModel> _managerModel;

        public LicenseInteractionModule(IMongoRepository<LicenseModel> licenseModel,
            IMongoRepository<EventModel> managerModel)
        {
            _licenseModel = licenseModel;
            _managerModel = managerModel;
        }

        [SlashCommand("install", "Install license")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task Install()
        {
            var discord = await _licenseModel.FindOneAsync(x => x.DiscordId == Context.Guild.Id);
            if (discord != null) await RespondAsync($"License has already been installed");
            if (discord == null)
            {
                discord = new LicenseModel
                {
                    DiscordId = Context.Guild.Id,
                    AdminId = Context.User.Id,
                    ExpireAt = DateTime.UtcNow
                };
                await Context.Guild.CreateRoleAsync("Manager", color: Color.Teal, isMentionable: false);
                await _licenseModel.InsertOneAsync(discord);
                await RespondAsync($"**License:** {discord.Id}");
            }
        }

        [SlashCommand("license", "Check license")]
        [RequireRole("Manager")]
        public async Task License()
        {
            var discord = await _licenseModel.FindOneAsync(x => x.DiscordId == Context.Guild.Id);
            if (discord != null) await RespondAsync($"**License:** {discord.Id}\n**Expire:** {discord.ExpireAt}");
            if (discord == null) await RespondAsync($"License not found.");
        }
    }
}
