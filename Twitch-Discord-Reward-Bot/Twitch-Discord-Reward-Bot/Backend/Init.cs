using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Twitch_Discord_Reward_Bot.Backend
{
    public static class Init
    {
        public static Newtonsoft.Json.Linq.JToken MasterConfig = Data.FileHandler.ReadJSON("./Data/Master.config.json");
        static List<BotInstance> Instances = new List<BotInstance> { };
        public static void Start()
        {
            foreach (Newtonsoft.Json.Linq.JToken Currency in Data.APIIntergrations.RewardCurrencyAPI.WebRequests.GetRequest("currency/all").Data)
            {
                Instances.Add(new BotInstance(Currency));
            }
        }
    }

    public class BotInstance
    {
        public Backend.Bots.DiscordBot.Instance DiscordBot;
        public Backend.Bots.TwitchBot.Instance TwitchBot;
        public Backend.Bots.Commands.CommandHandler CommandHandler;
        public Newtonsoft.Json.Linq.JToken CommandConfig, LoginConfig;

        public BotInstance(Newtonsoft.Json.Linq.JToken Currency)
        {
            CommandHandler = new Bots.Commands.CommandHandler(this);
            DiscordBot = new Backend.Bots.DiscordBot.Instance(this);
            TwitchBot = new Backend.Bots.TwitchBot.Instance(this);
        }

        public void LoadConfig()
        {
            //CommandConfig = Data.FileHandler.ReadJSON(ConfigPath + "/Command.config.json");
            //LoginConfig = Data.FileHandler.ReadJSON(ConfigPath + "/Login.config.json");
        }
    }
}
