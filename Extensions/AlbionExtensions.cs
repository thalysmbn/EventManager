using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using EventManager.Database;
using EventManager.Models;
using EventManager.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventManager.Extensions
{
    public static class AlbionExtensions
    {
        public static async Task<LanguageData> GetOrAddLanguageByRegion(this IMongoRepository<RegionModel> region, ulong discordId)
        {
            var regionModel = await region.FindOneAsync(x => x.DiscordId == discordId);
            if (regionModel == null)
            {
                await region.InsertOneAsync(new RegionModel
                {
                    DiscordId = discordId,
                    Region = "en"
                });
                return Languages.Language["en"];
            }
            return Languages.Language[regionModel.Region];
        }

        public static Embed CreateEventBuild(this EmbedBuilder embed, LanguageData language, Event eventDataModel)
        {
            embed.Title = $"{language.Event} ID #{eventDataModel.EventId}";
            embed.Color = eventDataModel.IsStopped ? Color.Red : eventDataModel.IsPaused ? Color.Orange : Color.Green;
            embed.Description = $"Manager: <@{eventDataModel.Manager}> \n";

            embed.AddField($"{language.EventTax}:", $"{eventDataModel.EventTax}%", true)
                .AddField($"{language.BuyerTax}:", $"{eventDataModel.BuyerTax}%", true)
                .AddField($"{language.Amount}:", string.Format("{0:#,##0}", eventDataModel.Amount), true)
                .AddField($"{language.CreatedAt}:", eventDataModel.CreatedAt, false)
                .AddField($"{language.EndedAt}:", eventDataModel.EndedAt != eventDataModel.CreatedAt ? eventDataModel.EndedAt : "-", false);
            return embed.Build();
        }

        public static Embed CreatePublicEventBuild(this EmbedBuilder embed, LanguageData language, Event eventDataModel)
        {
            embed.Title = $"{language.Event} ID #{eventDataModel.EventId}";
            embed.Color = eventDataModel.IsStopped ? Color.Red : eventDataModel.IsPaused ? Color.Orange : Color.Green;
            embed.Description = $"Manager: <@{eventDataModel.Manager}> \n";

            embed.AddField($"{language.EventTax}:", $"{eventDataModel.EventTax}%", true)
                .AddField($"{language.BuyerTax}:", $"{eventDataModel.BuyerTax}%", true)
                .AddField($"{language.Amount}:", string.Format("{0:#,##0}", eventDataModel.Amount), true);
            return embed.Build();
        }

        public static MessageComponent CreatePublicEventComponentBuilder(this ComponentBuilder component, LanguageData language, Event eventDataModel)
        {
            component
                    .WithButton($"{language.Join}", $"join#{eventDataModel.EventId}", ButtonStyle.Success, new Emoji("🎮"), disabled: eventDataModel.IsStopped);
            return component.Build();
        }

        public static MessageComponent CreateEventComponentBuilder(this ComponentBuilder component, LanguageData language, Event eventDataModel)
        {
            component
                    .WithButton($"{language.StartOrPauseEvent}", $"startOrPause#{eventDataModel.EventId}", ButtonStyle.Success, new Emoji("⏰"), disabled: eventDataModel.IsStopped)
                    .WithButton($"{language.EventTax}", $"eventTax#{eventDataModel.EventId}", ButtonStyle.Secondary, new Emoji("💵"), disabled: eventDataModel.IsStopped)
                    .WithButton($"{language.Amount}", $"amount#{eventDataModel.EventId}", ButtonStyle.Secondary, new Emoji("💰"), disabled: eventDataModel.IsStopped)
                    .WithButton($"{language.Users}", $"users#{eventDataModel.EventId}", ButtonStyle.Secondary, new Emoji("🔖"))
                    .WithButton($"{language.StopEvent}", $"stop#{eventDataModel.EventId}", ButtonStyle.Danger, disabled: eventDataModel.IsStopped);
            return component.Build();
        }

        public static async Task<bool> CheckLicense(this IMongoRepository<LicenseModel> license, LanguageData language, SocketModal modal)
        {
            var model = await license.FindOneAsync(x => x.DiscordId == modal.GuildId);
            if (model == null)
            {
                await modal.RespondAsync(language.LicenseNotFound);
                return false;
            }
            if (!model.IsValid)
            {
                await modal.RespondAsync($"{language.LicenseExpired}: **{model.ExpireAt}**");
                return false;
            }
            return true;
        }

        public static async Task<bool> CheckLicense(this IMongoRepository<LicenseModel> license, LanguageData language, SocketCommandContext context)
        {
            var model = await license.FindOneAsync(x => x.DiscordId == context.Guild.Id);
            if (model == null)
            {
                await context.Channel.SendMessageAsync(language.LicenseNotFound);
                return false;
            }
            if (!model.IsValid)
            {
                await context.Channel.SendMessageAsync($"{language.LicenseExpired}: **{model.ExpireAt}**");
                return false;
            }
            return true;
        }

        public static async Task<bool> CheckLicense(this IMongoRepository<LicenseModel> license, LanguageData language, SocketMessageComponent component)
        {
            var model = await license.FindOneAsync(x => x.DiscordId == component.GuildId);
            if (model == null)
            {
                await component.RespondAsync(language.LicenseNotFound);
                return false;
            }
            if (!model.IsValid)
            {
                await component.RespondAsync($"{language.LicenseExpired}: **{model.ExpireAt}**");
                return false;
            }
            return true;
        }

        public static async Task<bool> CheckLicense(this IMongoRepository<LicenseModel> license, LanguageData language, SocketInteractionContext context)
        {
            var model = await license.FindOneAsync(x => x.DiscordId == context.Guild.Id);
            if (model == null)
            {
                await context.Interaction.RespondAsync(language.LicenseNotFound);
                return false;
            }
            if (!model.IsValid)
            {
                await context.Interaction.RespondAsync($"{language.LicenseExpired}: **{model.ExpireAt}**");
                return false;
            }
            return true;
        }
    }
}
