using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using System.IO;

namespace DiscoNunu
{
    class Program
    {

        // Discord.Net heavily utilizes TAP for async, so we create
        // an asynchronous context from the beginning.
        static void Main(string[] args)
        {
            NunuConfig config = NunuConfigReader.ReadFromFile("nunuConfig.json");
            new DiscoNunu(config).MainAsync().GetAwaiter().GetResult();
        }

        
    }
}
