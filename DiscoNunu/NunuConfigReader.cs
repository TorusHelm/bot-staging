using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace DiscoNunu
{
    class NunuConfig {
        public string Token;
        public Dictionary<string, ServerConfig> Servers;
        public string Prefix;
        public string MainChannel;
        public bool WriteResponse;
        public bool DeleteCommand;
    }

    class ServerConfig
    {
        public string Name;
        public List<string> CanTake;
        public List<string> CanRelease;
        public string TakenEmoji;
        public string FreeEmoji;
        public string TakeReaction;
        public string NoticeReaction;
        public int MaxTime;
        public int DefaultTime;
    }

    class NunuConfigReader
    {

        public static NunuConfig ReadFromFile(string path) {
            var rawConfig = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<NunuConfig>(rawConfig);
        }
    }
}
