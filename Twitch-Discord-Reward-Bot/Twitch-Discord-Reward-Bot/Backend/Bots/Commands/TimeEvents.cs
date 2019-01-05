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
        public void Start()
        {
            new Thread(async () => await TimeThread()).Start();
        }

        public async Task TimeThread()
        {
            while (true){
                await Fish();
                System.Threading.Thread.Sleep(10000);
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
