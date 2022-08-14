using Discord;
using Discord.Interactions;
using EventManager.Database;
using EventManager.Extensions;
using EventManager.Models;
using EventManager.Resources;
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
        private readonly IMongoRepository<RegionModel> _regionRepository;

        public LicenseInteractionModule(IMongoRepository<LicenseModel> licenseModel,
            IMongoRepository<EventModel> managerModel,
            IMongoRepository<RegionModel> regionRepository)
        {
            _licenseModel = licenseModel;
            _managerModel = managerModel;
            _regionRepository = regionRepository;
        }

        [SlashCommand("install", "Install license")]
        public async Task Install()
        {
            var language = await _regionRepository.GetOrAddLanguageByRegion(Context.Guild.Id);
            var discord = await _licenseModel.FindOneAsync(x => x.DiscordId == Context.Guild.Id);
            if (discord != null) await RespondAsync(language.LicenseHasAlreadyBeenInstalled);
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
                await RespondAsync($"**{language.License}:** {discord.Id}");
            }
        }

        [SlashCommand("license", "Check license")]
        public async Task License()
        {
            var _user = Context.Guild.GetUser(Context.User.Id);
            if (_user == null) return;

            if (!_user.Roles.Any(x => x.Name == "Manager")) return;

            var language = await _regionRepository.GetOrAddLanguageByRegion(Context.Guild.Id);
            var discord = await _licenseModel.FindOneAsync(x => x.DiscordId == Context.Guild.Id);
            if (discord != null) await RespondAsync($"**{language.License}:** {discord.Id}\n**{language.Expire}:** {discord.ExpireAt}");
            if (discord == null) await RespondAsync($"{language.LicenseNotFound};");
        }
    }
}
