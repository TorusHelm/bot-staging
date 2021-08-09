using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using System.Linq;


namespace DiscoNunu
{

    class CommandService
    {

        private string _prefix;
        Dictionary<string, ServerConfig> _config;
        ServerManager _manager;

        private List<string> _takeCommand = new List<string>() { "take", "t" };
        private List<string> _releaseCommand = new List<string>() { "release", "r" };
        private List<string> _startCommand = new List<string>() { "start", "s" };

        public CommandService(string prefix, Dictionary<string, ServerConfig> config, ServerManager manager) {
            _prefix = prefix;
            _config = config;
            _manager = manager;
        }

        private bool ValidateRole(SocketGuildUser user, string server, string commandAccess) {
            List<string> roles = new List<string>();
            if (commandAccess == "canTake")
            {
                roles = _config[server].CanTake;
                return roles.Intersect(user.Roles.Select(x => x.Name)).Any() || roles.Count == 0;
            }
            else if (commandAccess == "canRelease")
            {
                roles = _config[server].CanRelease;
                return roles.Intersect(user.Roles.Select(x => x.Name)).Any() || _manager.State[server]?.User?.Id == user.Id || roles.Count == 0;
            }
            else
                return true;
        }

        private string OnRelease(SocketGuildUser user, List<string> args)
        {
            try
            {
                string server = GetArgsRelease(args);
                ValidateRole(user, server, "canRelease");
                return _manager.ReleaseServer(user, server);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private string OnTake(SocketGuildUser user, List<string> args)
        {
            try
            {
                (string server, int time) = GetArgsTake(args);
                if (!ValidateRole(user, server, "canTake"))
                    throw new Exception("Не хватает прав на операцию");
                return _manager.TakeServer(user, server, time);
            }
            catch (Exception ex) {
                return ex.Message;
            }
        }

        private string GetArgsRelease(List<string> args) {
            if (_config.ContainsKey(args[0]) || _config.Select(x => x.Value.Name).Contains(args[0]))
                return _config.ContainsKey(args[0]) ? args[0] : _config.First(x => x.Value.Name == args[0]).Key;
            else
                throw new Exception("Не найден сервер с таким именем");
        }

        private (string, int) GetArgsTake(List<string> args) {
            var server = GetArgsRelease(args);
            var time = -1;
            var index = 1;
            if (args.Count >= index + 1)
            {
                if (!int.TryParse(args[index], out time))
                    throw new Exception("Время удержания сервера указано некорректно");
            }
            else
            {
                time = _config[server].DefaultTime;
            }
            if (time > _config[server].MaxTime)
            {
                throw new Exception($"Сервер {server} нельзя удерживать так долго");
            }
            return (server, time);
        }

        public string OnCommand(SocketMessage message) {
            try
            {
                var rawCommand = message.Content.Split().ToList();
                var index = 0;
                var user = message.Author as SocketGuildUser;
                if (!string.IsNullOrEmpty(_prefix))
                    index++;
                if (_startCommand.Contains(rawCommand[index]))
                    return "Start";
                if (_takeCommand.Contains(rawCommand[index]))
                {
                    return OnTake(user, rawCommand.GetRange(++index, rawCommand.Count - index));
                }
                else if (_releaseCommand.Contains(rawCommand[index]))
                    return OnRelease(user, rawCommand.GetRange(++index, rawCommand.Count - index));
                else
                {
                    return $"Не удалось распознать команду {rawCommand[index++]}";
                }
            }
            catch(Exception ex)
            {
                return "Произошла ошибка: " + ex.Message;
            }
        }
    }
}
