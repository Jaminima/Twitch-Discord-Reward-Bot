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
            foreach (Newtonsoft.Json.Linq.JToken Currency in Data.APIIntergrations.RewardCurrencyAPI.WebRequests.PostRequest("currency/all",null,true).Data)
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
            Data.APIIntergrations.RewardCurrencyAPI.Objects.Currency C = Data.APIIntergrations.RewardCurrencyAPI.Objects.Currency.FromJson(Currency);
            CommandConfig = C.CommandConfig;
            LoginConfig = C.LoginConfig;
            CommandHandler = new Bots.Commands.CommandHandler(this);
            try { DiscordBot = new Backend.Bots.DiscordBot.Instance(this); } catch { }
            try { TwitchBot = new Backend.Bots.TwitchBot.Instance(this); } catch { }
        }
    }
}
