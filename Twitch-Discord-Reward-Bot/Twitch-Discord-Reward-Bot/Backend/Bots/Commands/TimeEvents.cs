using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Twitch_Discord_Reward_Bot.Backend.Bots.Commands
{
    public class TimeEvents
    {
        BotInstance BotInstance;
        public void Start(BotInstance BotInstance)
        {
            this.BotInstance = BotInstance;
            new Thread(async () => await TimeThread()).Start();
        }

        public async Task TimeThread()
        {
            while (true){
                await Fish();
                await AutoMessage();
                System.Threading.Thread.Sleep(10000);
            }
        }

        public Dictionary<int, DateTime> MessageHistory = new Dictionary<int, DateTime> { };
        public DateTime MessageLast = DateTime.MinValue;
        public async Task AutoMessage()
        {
            if (BotInstance.CommandConfig["AutoMessage"]["RequireLive"].ToString().ToLower() == "true")
            {
                if (Data.APIIntergrations.Twitch.IsLive(BotInstance) == false) { return; }
            }
            int MinDelay = int.Parse(BotInstance.CommandConfig["AutoMessage"]["MinimumDelay"].ToString());
            if (((TimeSpan)(DateTime.Now - MessageLast)).TotalSeconds >= MinDelay)
            {
                Newtonsoft.Json.Linq.JToken Items = BotInstance.CommandConfig["AutoMessage"]["Messages"];
                for (int i = 0; i < Items.Count(); i++)
                {
                    bool ShouldSend = false;
                    if (MessageHistory.ContainsKey(i))
                    {
                        if (((TimeSpan)(DateTime.Now - MessageHistory[i])).TotalSeconds >= int.Parse(Items[i]["Delay"].ToString())) { ShouldSend = true; }
                    }
                    else { ShouldSend = true; }
                    if (ShouldSend)
                    {
                        if (BotInstance.CommandHandler.CommandEnabled(BotInstance.CommandConfig["AutoMessage"], MessageType.Twitch))
                        { BotInstance.TwitchBot.Client.SendMessage(BotInstance.CommandConfig["ChannelName"].ToString(), Items[i]["Body"].ToString()); }
                        MessageLast = DateTime.Now;
                        MessageHistory.Add(i, DateTime.Now);
                    }
                }
            }
        }

        public Dictionary<DateTime, Fisherman> Fishermen = new Dictionary<DateTime, Fisherman> { };
        public async Task Fish()
        {
            List<DateTime> FishToRemove=new List<DateTime> { };
            IEnumerable<KeyValuePair<DateTime, Fisherman>> FinishedFishermen = Fishermen.Where(x => ((TimeSpan)(DateTime.Now - x.Key)).TotalSeconds >= x.Value.SecondsToFish);
            foreach (KeyValuePair<DateTime,Fisherman> Fisher in FinishedFishermen)
            {
                Newtonsoft.Json.Linq.JToken Items = Fisher.Value.BotInstance.CommandConfig["CommandSetup"]["Fish"]["Items"], ChosenItem = Items[Init.Rnd.Next(0,Items.Count())];
                int TotalChance = 0,ChosenChance=-1;
                foreach (Newtonsoft.Json.Linq.JToken Item in Items) { TotalChance += int.Parse(Item["Chance"].ToString()); }
                ChosenChance = Init.Rnd.Next(0, TotalChance); TotalChance = 0;
                foreach (Newtonsoft.Json.Linq.JToken Item in Items) { TotalChance += int.Parse(Item["Chance"].ToString()); if (TotalChance >= ChosenChance) { ChosenItem = Item; break; } }
                Data.APIIntergrations.RewardCurrencyAPI.Objects.Bank B = Data.APIIntergrations.RewardCurrencyAPI.Objects.Bank.FromTwitchDiscord(Fisher.Value.e, Fisher.Value.BotInstance, Fisher.Value.e.SenderID);
                if (Data.APIIntergrations.RewardCurrencyAPI.Objects.Bank.AdjustBalance(B, int.Parse(ChosenItem["Reward"].ToString()), "+"))
                {
                    await Fisher.Value.BotInstance.CommandHandler.SendMessage(Fisher.Value.BotInstance.CommandConfig["CommandSetup"]["Fish"]["Responses"]["Finished"].ToString(), Fisher.Value.e,null,int.Parse(ChosenItem["Reward"].ToString()),-1, ChosenItem["Name"].ToString());
                    FishToRemove.Add(Fisher.Key);
                }
            }
            foreach (DateTime FishKey in FishToRemove)
            {
                Fishermen.Remove(FishKey);
            }
        }
    }

    public class Fisherman
    {
        public StandardisedMessageRequest e;
        public BotInstance BotInstance;
        public int SecondsToFish;

        public Fisherman(StandardisedMessageRequest e,BotInstance BotInstance)
        {
            this.e = e;
            this.BotInstance = BotInstance;
            int MinTime = int.Parse(BotInstance.CommandConfig["CommandSetup"]["Fish"]["MinTime"].ToString()),
                MaxTime = int.Parse(BotInstance.CommandConfig["CommandSetup"]["Fish"]["MaxTime"].ToString());
            SecondsToFish = Init.Rnd.Next(MinTime, MaxTime);
        }
    }
}
