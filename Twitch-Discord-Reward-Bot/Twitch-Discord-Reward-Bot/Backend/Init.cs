using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Twitch_Discord_Reward_Bot.Backend
{
    public static class Init
    {
        public static Random Rnd = new Random();
        public static Newtonsoft.Json.Linq.JToken MasterConfig = Data.FileHandler.ReadJSON("./Data/Master.config.json");
        static Dictionary<int,BotInstance> Instances = new Dictionary<int, BotInstance> { };
        public static void Start()
        {
            string S = Data.APIIntergrations.RewardCurrencyAPI.WebRequests.GetAuthToken();
            if (S == null) { Console.WriteLine("API Tokens misconfigured!"); }
            else
            {
                while (true)
                {
                    foreach (Newtonsoft.Json.Linq.JToken Currency in Data.APIIntergrations.RewardCurrencyAPI.WebRequests.PostRequest("currency/all", null, true).Data)
                    {
                        Data.APIIntergrations.RewardCurrencyAPI.Objects.Currency C = Data.APIIntergrations.RewardCurrencyAPI.Objects.Currency.FromJson(Currency);
                        if (C.CommandConfig.Count() != 0 && C.LoginConfig.Count() != 0)
                        {
                            if (C.CommandConfig["BotsEnabled"].ToString() == "True")
                            {
                                if (!Instances.Keys.Contains(C.ID)) { Instances.Add(C.ID, new BotInstance(C)); }
                                else
                                {
                                    Instances[C.ID].CommandConfig = C.CommandConfig;
                                    Instances[C.ID].LoginConfig = C.LoginConfig;
                                    Instances[C.ID].Currency = C;
                                    Instances[C.ID].Start();
                                }
                            }
                            else if (Instances.Keys.Contains(C.ID))
                            {
                                Instances[C.ID].Stop();
                            }
                        }
                        else { Console.WriteLine("There was a error relating to currency configs"); }
                    }
                    System.Threading.Thread.Sleep(300000);
                }
            }
        }
    }

    public class BotInstance
    {
        public Data.APIIntergrations.RewardCurrencyAPI.Objects.Currency Currency;
        public Backend.Bots.DiscordBot.Instance DiscordBot;
        public Backend.Bots.TwitchBot.Instance TwitchBot;
        public Backend.Bots.Commands.CommandHandler CommandHandler;
        public Bots.Commands.TimeEvents TimeEvents;
        public Newtonsoft.Json.Linq.JToken CommandConfig, LoginConfig;
        public Dictionary<string, Data.APIIntergrations.AccessToken> AccessTokens = new Dictionary<string, Data.APIIntergrations.AccessToken> { };
        public bool Isrunning = false;

        public BotInstance(Data.APIIntergrations.RewardCurrencyAPI.Objects.Currency Currency)
        {
            this.Currency = Currency;
            this.CommandConfig = this.Currency.CommandConfig;
            this.LoginConfig = this.Currency.LoginConfig;
            //new Thread(() => CheckBotsAlive()).Start();
            Start();
        }

        public void Start()
        {
            //if (Isrunning) { CheckBotsAlive(); return; }
            //Isrunning = true;
            if (this.CommandHandler == null) { this.CommandHandler = new Bots.Commands.CommandHandler(this); }
            try { if (this.DiscordBot == null) { this.DiscordBot = new Backend.Bots.DiscordBot.Instance(this); } } catch { }
            try { if (this.TwitchBot == null) { this.TwitchBot = new Backend.Bots.TwitchBot.Instance(this); } } catch { }
            System.Threading.Thread.Sleep(5000);
            if (this.TimeEvents == null)
            {
                /*this.TimeEvents.Stop();*/
                this.TimeEvents = new Bots.Commands.TimeEvents();
                this.TimeEvents.Start(this);
            }
        }

        public void CheckBotsAlive()
        {
            while (true)
            {
                Thread.Sleep(10000);
                if (Isrunning)
                {
                    if (!TwitchBot.Client.IsConnected) { try { TwitchBot.Client.Connect(); } catch { } }
                    if (DiscordBot.Client.ConnectionState == Discord.ConnectionState.Disconnected) { try { DiscordBot.Client.StartAsync(); } catch { } }
                }
            }
        }

        public void Stop()
        {
            if (!Isrunning) { return; }
            Isrunning = false;
            DiscordBot.Client.StopAsync();
            TwitchBot.Client.Disconnect();
            TimeEvents.Stop();
            this.CommandHandler = null;
            TimeEvents = null;
            DiscordBot = null;
            TwitchBot = null;
            Console.WriteLine("Stopped " + Currency.ID + " Bots");
        }
    }
}
