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
        static Dictionary<int,BotInstance> Instances = new Dictionary<int, BotInstance> { };
        public static void Start()
        {
            while (true)
            {
                foreach (Newtonsoft.Json.Linq.JToken Currency in Data.APIIntergrations.RewardCurrencyAPI.WebRequests.PostRequest("currency/all", null, true).Data)
                {
                    Data.APIIntergrations.RewardCurrencyAPI.Objects.Currency C = Data.APIIntergrations.RewardCurrencyAPI.Objects.Currency.FromJson(Currency);
                    if (!Instances.Keys.Contains(C.ID)) { Instances.Add(C.ID, new BotInstance(C)); }
                }
                System.Threading.Thread.Sleep(60000);
            }
        }
    }

    public class BotInstance
    {
        public Data.APIIntergrations.RewardCurrencyAPI.Objects.Currency Currency;
        public Backend.Bots.DiscordBot.Instance DiscordBot;
        public Backend.Bots.TwitchBot.Instance TwitchBot;
        public Backend.Bots.Commands.CommandHandler CommandHandler;
        public Newtonsoft.Json.Linq.JToken CommandConfig, LoginConfig;

        public BotInstance(Data.APIIntergrations.RewardCurrencyAPI.Objects.Currency Currency)
        {
            this.Currency = Currency;
            CommandConfig = Currency.CommandConfig;
            LoginConfig = Currency.LoginConfig;
            CommandHandler = new Bots.Commands.CommandHandler(this);
            try { DiscordBot = new Backend.Bots.DiscordBot.Instance(this); } catch { }
            try { TwitchBot = new Backend.Bots.TwitchBot.Instance(this); } catch { }
        }
    }
}
