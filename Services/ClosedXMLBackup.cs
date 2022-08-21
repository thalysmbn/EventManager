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

                var worksheetData = workbook.Worksheets.Add("Data");

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

                worksheetData.Cell($"A1").Value = "Settings";
                worksheetData.Cell($"A1").Style.Font.SetBold();
                worksheetData.Cell($"A1").Style.Fill.SetBackgroundColor(XLColor.Gray);
                worksheetData.Cell($"B1").Style.Fill.SetBackgroundColor(XLColor.Gray);
                worksheetData.Cell($"A1").WorksheetColumn().Width = 15;
                worksheetData.Cell($"B1").WorksheetColumn().Width = 30;

                foreach (var setting in settings.Select((value, index) => new { value, index }))
                {
                    worksheetData.Cell($"A{setting.index + 2}").Value = setting.value.Key;
                    worksheetData.Cell($"B{setting.index + 2}").Value = string.Format("{0:#,##0}", setting.value.Value);
                    worksheetData.Cell($"A{setting.index + 2}").Style.Fill.SetBackgroundColor(XLColor.Gray);
                    worksheetData.Cell($"B{setting.index + 2}").Style.Fill.SetBackgroundColor(XLColor.Gray);
                    worksheetData.Cell($"B{setting.index + 2}").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
                }

                var users = discord.Users.OrderByDescending(x => x.Amount);
                var usersCount = users.Count();

                worksheetData.Cell($"D1").Value = "Users";
                worksheetData.Cell($"F1").Value = usersCount;
                worksheetData.Cell($"D1").Style.Font.SetBold();
                worksheetData.Cell($"F1").Style.Font.SetBold();
                worksheetData.Cell($"D1").Style.Fill.SetBackgroundColor(XLColor.GreenPigment);
                worksheetData.Cell($"E1").Style.Fill.SetBackgroundColor(XLColor.GreenPigment);
                worksheetData.Cell($"F1").Style.Fill.SetBackgroundColor(XLColor.GreenPigment);
                worksheetData.Cell($"D1").WorksheetColumn().Width = 15;
                worksheetData.Cell($"E1").WorksheetColumn().Width = 15;
                worksheetData.Cell($"F1").WorksheetColumn().Width = 15;

                foreach (var setting in users.Select((value, index) => new { value, index }))
                {
                    var user = _discord.GetUser(setting.value.UserId);
                    worksheetData.Cell($"D{setting.index + 2}").Value = string.Format("{0:#,##0}", setting.value.UserId);
                    worksheetData.Cell($"E{setting.index + 2}").Value = user == null ? "" : user.Nickname == null ? user.Username : user.Nickname;
                    worksheetData.Cell($"F{setting.index + 2}").Value = string.Format("{0:#,##0}", setting.value.Amount);
                    worksheetData.Cell($"E{setting.index + 2}").Style.Font.SetBold();
                    worksheetData.Cell($"D{setting.index + 2}").Style.Fill.SetBackgroundColor(XLColor.GreenPigment);
                    worksheetData.Cell($"E{setting.index + 2}").Style.Fill.SetBackgroundColor(XLColor.GreenPigment);
                    worksheetData.Cell($"F{setting.index + 2}").Style.Fill.SetBackgroundColor(XLColor.GreenPigment);
                    worksheetData.Cell($"D{setting.index + 2}").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
                    worksheetData.Cell($"F{setting.index + 2}").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
                    worksheetData.Cell($"D{setting.index + 2}").WorksheetColumn().Width = 30;
                }
                worksheetData.Cell($"D{usersCount + 2}").Value = "Total";
                worksheetData.Cell($"D{usersCount + 2}").Style.Font.SetBold();
                worksheetData.Cell($"F{usersCount + 2}").Style.Font.SetBold();
                worksheetData.Cell($"F{usersCount + 2}").Value = string.Format("{0:#,##0}", users.Sum(x => x.Amount));
                worksheetData.Cell($"D{usersCount + 2}").Style.Fill.SetBackgroundColor(XLColor.GreenPigment);
                worksheetData.Cell($"E{usersCount + 2}").Style.Fill.SetBackgroundColor(XLColor.GreenPigment);
                worksheetData.Cell($"F{usersCount + 2}").Style.Fill.SetBackgroundColor(XLColor.GreenPigment);
                worksheetData.Cell($"F{usersCount + 2}").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);

                workbook.SaveAs($"data\\{discord.DiscordId} - {_discord.Name}.xlsx");
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
        }
    }
}
