using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;

namespace DiscoNunu
{
    class DiscoNunu
    {
        private readonly DiscordSocketClient _client;

        private readonly NunuConfig _config;
        private CommandService _commandService;
        private ServerManager _serverManager;
        private IMessageChannel _currentChannel;
        private IGuild _currentGuild;

        public DiscoNunu(NunuConfig config)
        {
            // It is recommended to Dispose of a client when you are finished
            // using it, at the end of your app's lifetime.
            _client = new DiscordSocketClient();
            _config = config;
            _serverManager = new ServerManager(_config.Servers, SendReleaseMessage);
            _commandService = new CommandService(_config.Prefix, config.Servers, _serverManager);
            _client.Log += LogAsync;
            _client.Ready += ReadyAsync;
            _client.MessageReceived += MessageReceivedAsync;
        }

        public async Task MainAsync()
        {
            // Tokens should be considered secret data, and never hard-coded.
            await _client.LoginAsync(TokenType.Bot, _config.Token);
            await _client.StartAsync();

            // Block the program until it is closed.
            await Task.Delay(Timeout.Infinite);
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        // The Ready event indicates that the client has opened a
        // connection and it is now safe to access the cache.
        private Task ReadyAsync()
        {
            Console.WriteLine($"{_client.CurrentUser} is connected!");

            return Task.CompletedTask;
        }

        // This is not the recommended way to write a bot - consider
        // reading over the Commands Framework sample.
        private async Task MessageReceivedAsync(SocketMessage message)
        {
            if (message.Content.StartsWith(_config.Prefix))
            {
                // The bot should never respond to itself.
                if (message.Author.Id == _client.CurrentUser.Id)
                    return;
                _currentChannel = message.Channel;
                _currentGuild = (message.Channel as SocketGuildChannel).Guild;
                var response = _commandService.OnCommand(message);
                if (_config.WriteResponse)
                    await message.Channel.SendMessageAsync(response);
                await SendMainMessage();
                if (_config.DeleteCommand)
                    await message.DeleteAsync();
            }
        }

        private async Task SendReleaseMessage(string message) {
            if(_config.WriteResponse)
                await _currentChannel.SendMessageAsync(message);
            await SendMainMessage();
        }

        private async Task SendMainMessage() {
            var message = "";
            var state = "";
            foreach (var conf in _serverManager.State) {
                var takenMessage = conf.Value.IsTaken ? string.Format("занят {0} до {1}", conf.Value.User, conf.Value.ReleaseTime.ToString("HH:mm")) : "свободен";
                message += string.Format("{0} {1}\n", conf.Value.ServerConfig.Name, takenMessage);
            }
            foreach (var conf in _serverManager.State)
            {
                try
                {
                    var rawFreeEmoji = conf.Value.ServerConfig.FreeEmoji;
                    var freeEmoji = "<" + rawFreeEmoji + _currentGuild.Emotes.First(x => rawFreeEmoji.Contains(x.Name)).Id + ">";
                    var rawTakenEmoji = conf.Value.ServerConfig.TakenEmoji;
                    var takenEmoji = "<" + rawTakenEmoji + _currentGuild.Emotes.First(x => rawTakenEmoji.Contains(x.Name)).Id + ">";
                    state += string.Format("{0} {1}       ", conf.Key, conf.Value.IsTaken ? $"{takenEmoji} {conf.Value.ReleaseTime.ToString("HH:mm")}" : freeEmoji);
                }
                catch (Exception ex)
                {
                    var freeEmoji = conf.Value.ServerConfig.FreeEmoji;
                    var takenEmoji = conf.Value.ServerConfig.TakenEmoji;
                    state += string.Format("{0} {1}       ", conf.Key, conf.Value.IsTaken ? $"{takenEmoji} {conf.Value.ReleaseTime.ToString("HH:mm")}" : freeEmoji);
                }
            }
            message += state;
            var channel = (await _currentGuild.GetTextChannelsAsync()).First(x => x.Name == _config.MainChannel);
            var messageToEdit = (await channel.GetPinnedMessagesAsync()).Where(x => x.Author.Id == _client.CurrentUser.Id).LastOrDefault();
            if (messageToEdit != null)
                await channel.ModifyMessageAsync(messageToEdit.Id, (prop) => prop.Content = message);
            else
            {
                var mes = await channel.SendMessageAsync(message);
                await mes.PinAsync();
            }
            
        }
    }
}
