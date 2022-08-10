using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using EventManager.Database;
using EventManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventManager.Extensions
{
    public static class AlbionExtensions
    {
        public static Embed CreateEventBuild(this EmbedBuilder embed, Event eventDataModel)
        {
            embed.Title = $"Event ID #{eventDataModel.EventId}";
            embed.Color = eventDataModel.IsStopped ? Color.Red : eventDataModel.IsPaused ? Color.Orange : Color.Green;
            embed.Description = $"Manager: <@{eventDataModel.Manager}> \n";

            embed.AddField("Event Tax:", $"{eventDataModel.EventTax}%", true)
                .AddField("Buyer Tax:", $"{eventDataModel.BuyerTax}%", true)
                .AddField("Amount:", string.Format("{0:#,##0}", eventDataModel.Amount), true)
                .AddField("Created At:", eventDataModel.CreatedAt, false)
                .AddField("Ended At:", eventDataModel.EndedAt != eventDataModel.CreatedAt ? eventDataModel.EndedAt : "-", false);
            return embed.Build();
        }

        public static Embed CreatePublicEventBuild(this EmbedBuilder embed, Event eventDataModel)
        {
            embed.Title = $"Event ID #{eventDataModel.EventId}";
            embed.Color = eventDataModel.IsStopped ? Color.Red : eventDataModel.IsPaused ? Color.Orange : Color.Green;
            embed.Description = $"Manager: <@{eventDataModel.Manager}> \n";

            embed.AddField("Event Tax:", $"{eventDataModel.EventTax}%", true)
                .AddField("Buyer Tax:", $"{eventDataModel.BuyerTax}%", true)
                .AddField("Amount:", string.Format("{0:#,##0}", eventDataModel.Amount), true);
            return embed.Build();
        }

        public static MessageComponent CreatePublicEventComponentBuilder(this ComponentBuilder component, Event eventDataModel)
        {
            component
                    .WithButton("Join", $"join#{eventDataModel.EventId}", ButtonStyle.Success, new Emoji("🎮"), disabled: eventDataModel.IsStopped);
            return component.Build();
        }

        public static MessageComponent CreateEventComponentBuilder(this ComponentBuilder component, Event eventDataModel)
        {
            component
                    .WithButton("Start/Pause Event", $"startOrPause#{eventDataModel.EventId}", ButtonStyle.Success, new Emoji("⏰"), disabled: eventDataModel.IsStopped)
                    .WithButton("Event Tax", $"eventTax#{eventDataModel.EventId}", ButtonStyle.Secondary, new Emoji("💵"), disabled: eventDataModel.IsStopped)
                    .WithButton("Amount", $"amount#{eventDataModel.EventId}", ButtonStyle.Secondary, new Emoji("💰"), disabled: eventDataModel.IsStopped)
                    .WithButton("Users", $"users#{eventDataModel.EventId}", ButtonStyle.Secondary, new Emoji("🔖"))
                    .WithButton("Stop Event", $"stop#{eventDataModel.EventId}", ButtonStyle.Danger, disabled: eventDataModel.IsStopped);
            return component.Build();
        }

        public static async Task<bool> CheckLicense(this IMongoRepository<LicenseModel> license, SocketModal modal)
        {
            var model = await license.FindOneAsync(x => x.DiscordId == modal.GuildId);
            if (model == null)
            {
                await modal.RespondAsync("Not found.");
                return false;
            }
            if (!model.IsValid)
            {
                await modal.RespondAsync($"License expired: **{model.ExpireAt}**");
                return false;
            }
            return true;
        }

        public static async Task<bool> CheckLicense(this IMongoRepository<LicenseModel> license, SocketMessageComponent component)
        {
            var model = await license.FindOneAsync(x => x.DiscordId == component.GuildId);
            if (model == null)
            {
                await component.RespondAsync("Not found.");
                return false;
            }
            if (!model.IsValid)
            {
                await component.RespondAsync($"License expired: **{model.ExpireAt}**");
                return false;
            }
            return true;
        }

        public static async Task<bool> CheckLicense(this IMongoRepository<LicenseModel> license, SocketInteractionContext context)
        {
            var model = await license.FindOneAsync(x => x.DiscordId == context.Guild.Id);
            if (model == null)
            {
                await context.Interaction.RespondAsync("Not found.");
                return false;
            }
            if (!model.IsValid)
            {
                await context.Interaction.RespondAsync($"License expired: **{model.ExpireAt}**");
                return false;
            }
            return true;
        }
    }
}
