using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using EventManager.Configurations;
using EventManager.Handler;
using EventManager.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventManager.Services
{
    public class DiscordBOT : IHostedService
    {
        private readonly DiscordSocketClient _discordSocketClient;
        private readonly InteractionHandler _interactionHandler;
        private readonly PrefixHandler _prefixHandler;
        private readonly UserVoiceStateUpdatedHandler _userVoiceStateUpdatedHandler;
        private readonly IOptions<DiscordConfiguration> _discordConfiguration;

        public DiscordBOT(DiscordSocketClient discordSocketClient,
            InteractionService interactionService,
            InteractionHandler interactionHandler,
            PrefixHandler prefixHandler,
            ButtonExecutedHandler buttonExecutedHandler,
            ModalSubmittedHandler modalSubmittedHandler,
            UserVoiceStateUpdatedHandler userVoiceStateUpdatedHandler,
            IOptions<DiscordConfiguration> discordConfiguration)
        {
            _discordSocketClient = discordSocketClient;
            _discordConfiguration = discordConfiguration;
            _interactionHandler = interactionHandler;
            _prefixHandler = prefixHandler;
            _userVoiceStateUpdatedHandler = userVoiceStateUpdatedHandler;
            _discordSocketClient.Ready += async () =>
            {
                await interactionService.RegisterCommandsGloballyAsync(true);
            };
            _discordSocketClient.ButtonExecuted += buttonExecutedHandler.Executed;
            _discordSocketClient.ModalSubmitted += modalSubmittedHandler.Executed;
            _discordSocketClient.UserVoiceStateUpdated += _userVoiceStateUpdatedHandler.Executed;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _interactionHandler.InitializeAsync();
            await _prefixHandler.InitializeAsync();
            await _discordSocketClient.LoginAsync(TokenType.Bot, _discordConfiguration.Value.Token);
            await _discordSocketClient.StartAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _discordSocketClient.DisposeAsync();
        }
    }
}
