using ClosedXML.Excel;
using Discord.WebSocket;
using EventManager.Database;
using EventManager.Models;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventManager.Services
{
    public class ClosedXMLBackup : IHostedService
    {
        private readonly DiscordSocketClient _discordSocketClient;
        private readonly IMongoRepository<EventModel> _eventRepository;
        private System.Timers.Timer _timer { get; set; }
        public ClosedXMLBackup(DiscordSocketClient discordSocketClient,
            IMongoRepository<EventModel> eventRepository)
        {
            _discordSocketClient = discordSocketClient;
            _eventRepository = eventRepository;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var discords = _eventRepository.AsQueryable().ToList();

            foreach (var discord in discords)
            {
                var _discord = _discordSocketClient.GetGuild(discord.DiscordId);
                if (_discord == null) continue;

                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Sample Sheet");

                var settings = new Dictionary<string, object>();
                settings.Add("Discord ID", discord.DiscordId);
                settings.Add("Category ID", discord.CategoryId);
                settings.Add("Category Voice ID", discord.CategoryVoiceId);
                settings.Add("Queue Voice ID", discord.QueueVoiceId);
                settings.Add("Manager Channel ID", discord.ManagerChannelId);
                settings.Add("Event Channel ID", discord.EventChannelId);
                settings.Add("Wallet Channel ID", discord.WalletChannelId);
                settings.Add("Logs Channel ID", discord.LogChannelId);
                settings.Add("Last Event ID", discord.LastEventId);

                worksheet.Cell($"A1").Value = "Settings";
                worksheet.Cell($"A1").Style.Font.SetBold();
                worksheet.Cell($"A1").Style.Fill.SetBackgroundColor(XLColor.Gray);
                worksheet.Cell($"B1").Style.Fill.SetBackgroundColor(XLColor.Gray);

                foreach (var setting in settings.Select((value, index) => new { value, index }))
                {
                    worksheet.Cell($"A{setting.index + 2}").Value = setting.value.Key;
                    worksheet.Cell($"B{setting.index + 2}").Value = setting.value.Value;
                    worksheet.Cell($"A{setting.index + 2}").Style.Fill.SetBackgroundColor(XLColor.Gray);
                    worksheet.Cell($"B{setting.index + 2}").Style.Fill.SetBackgroundColor(XLColor.Gray);
                }

                var users = discord.Users.OrderByDescending(x => x.Amount);
                var usersCount = users.Count();

                worksheet.Cell($"D1").Value = "Users";
                worksheet.Cell($"F1").Value = usersCount;
                worksheet.Cell($"D1").Style.Font.SetBold();
                worksheet.Cell($"D1").Style.Fill.SetBackgroundColor(XLColor.GreenPigment);
                worksheet.Cell($"E1").Style.Fill.SetBackgroundColor(XLColor.GreenPigment);
                worksheet.Cell($"F1").Style.Fill.SetBackgroundColor(XLColor.GreenPigment);

                foreach (var setting in users.Select((value, index) => new { value, index }))
                {
                    var user = _discord.GetUser(setting.value.UserId);
                    worksheet.Cell($"D{setting.index + 2}").Value = setting.value.UserId;
                    worksheet.Cell($"E{setting.index + 2}").Value = user == null ? "" : user.Username;
                    worksheet.Cell($"F{setting.index + 2}").Value = string.Format("{0:#,##0}", setting.value.Amount);
                    worksheet.Cell($"D{setting.index + 2}").Style.Fill.SetBackgroundColor(XLColor.GreenPigment);
                    worksheet.Cell($"E{setting.index + 2}").Style.Fill.SetBackgroundColor(XLColor.GreenPigment);
                    worksheet.Cell($"F{setting.index + 2}").Style.Fill.SetBackgroundColor(XLColor.GreenPigment);
                }
                worksheet.Cell($"D{usersCount + 2}").Value = "Total";
                worksheet.Cell($"D{usersCount + 2}").Style.Font.SetBold();
                worksheet.Cell($"F{usersCount + 2}").Value = string.Format("{0:#,##0}", users.Sum(x => x.Amount));
                worksheet.Cell($"D{usersCount + 2}").Style.Fill.SetBackgroundColor(XLColor.GreenPigment);
                worksheet.Cell($"E{usersCount + 2}").Style.Fill.SetBackgroundColor(XLColor.GreenPigment);
                worksheet.Cell($"F{usersCount + 2}").Style.Fill.SetBackgroundColor(XLColor.GreenPigment);

                workbook.SaveAs($"data\\{discord.DiscordId} - {_discord.Name}.xlsx");
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
        }
    }
}
