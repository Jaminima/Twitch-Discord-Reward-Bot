using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Twitch_Discord_Reward_Bot.Backend
{
    public static class Init
    {
        static List<BotInstance> Instances = new List<BotInstance> { };
        public static void Start()
        {
            foreach (string Path in System.IO.Directory.GetDirectories("./Config/"))
            {
                if (!Path.ToLower().EndsWith("ignore") && !Path.ToLower().StartsWith("ignore"))
                {
                    Instances.Add(new BotInstance(Path));
                }
            }
        }
    }

    public class BotInstance
    {
        public Backend.Bots.DiscordBot.Instance DiscordBot;
        public Backend.Bots.TwitchBot.Instance TwitchBot;
        string ConfigPath;
        public Newtonsoft.Json.Linq.JToken CommandConfig, LoginConfig;

        public BotInstance(string ConfigPath)
        {
            this.ConfigPath = ConfigPath;
            LoadConfig();
            DiscordBot = new Backend.Bots.DiscordBot.Instance(this);
            TwitchBot = new Backend.Bots.TwitchBot.Instance(this);
        }

        public void LoadConfig()
        {
            CommandConfig = Data.FileHandler.ReadJSON(ConfigPath + "/Command.config.json");
            LoginConfig = Data.FileHandler.ReadJSON(ConfigPath + "/Login.config.json");
        }
    }
}
