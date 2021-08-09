using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using System.Linq;

namespace DiscoNunu
{
    class ServerState
    {
        public ServerState(ServerConfig config, Func<string, Task> releaseCallback) {
            ServerConfig = config;
            ReleaseTime = DateTime.MinValue;
            _releaseCallback = releaseCallback;
        }

        private Func<string, Task> _releaseCallback;
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationToken _token;

        public readonly ServerConfig ServerConfig;
        public bool IsTaken { get; private set; }
        public SocketGuildUser User { get; private set; }
        public DateTime ReleaseTime { get; private set; }
        public void TakeServer(SocketGuildUser user, int time)
        {
            if (!IsTaken && User == null && ReleaseTime == DateTime.MinValue)
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _token = _cancellationTokenSource.Token;
                User = user;
                IsTaken = true;
                ReleaseTime = DateTime.Now.AddMinutes(time);
                Thread thread1 = new Thread(() => {
                    Task t = Task.Run(() =>
                    {
                        Task.Delay(time * 60 * 1000).Wait();
                        ReleaseServer();
                        _releaseCallback($"Сервер {ServerConfig.Name} освободился по таймеру");
                    }, _token);
                });
                thread1.Start();
            }
            else
            {
                throw new Exception($"Сервер уже занят пользователем {User.Nickname}");
            }
        }

        public void ReleaseServer() {
            User = null;
            IsTaken = false;
            ReleaseTime = DateTime.MinValue;
            try
            {
                _cancellationTokenSource?.Cancel();
            }
            catch (Exception) { }
        }
    }

    class ServerManager
    {
        public Dictionary<string, ServerState> State { get; private set; }
        
        public ServerManager(Dictionary<string, ServerConfig> configList, Func<string, Task> releaseCallback) {
            State = new Dictionary<string, ServerState>();
            foreach (var config in configList) {
                State.Add(config.Key, new ServerState(config.Value, releaseCallback));
            }
        }

        public string TakeServer(SocketGuildUser user, string server, int time)
        {
            try
            {
                State[server].TakeServer(user, time);
                return $"{user.Nickname} занял сервер {server} на {time} минут";
            }
            catch (Exception ex)
            {
                return $"Произошла ошибка: {ex.Message}";
            }
        }

        public string ReleaseServer(SocketGuildUser user, string server) {
            try
            {
                State[server].ReleaseServer();
                return $"{user.Nickname} освободил {server}";
            }
            catch (Exception ex) {
                return $"Произошла ошибка: {ex.Message}";
            }
        }
    }
}
