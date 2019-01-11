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
                RemoveDuels();
                PerformRaffle();
                System.Threading.Thread.Sleep(10000);
            }
        }

        public int RaffleNumber=0;
        public List<Raffler> RaffleParticipants = new List<Raffler> { };
        public DateTime LastRaffle = DateTime.MinValue;
        public bool UserRaffleing(StandardisedUser User)
        {
            return RaffleParticipants.Where(x=>x.User.ID==User.ID).Count()!=0;
        }
        public void PerformRaffle()
        {
            if (BotInstance.CommandHandler.LiveCheck(BotInstance.CommandConfig["Raffle"]))
            {
                bool DoRaffle = false;
                int MinDelay = int.Parse(BotInstance.CommandConfig["Raffle"]["Triggers"]["Delay"].ToString());
                if (BotInstance.CommandConfig["Raffle"]["Triggers"]["OnMinuteOfHour"].Count() != 0)
                {
                    if (BotInstance.CommandHandler.JArrayContainsString(BotInstance.CommandConfig["Raffle"]["Triggers"]["OnMinuteOfHour"], DateTime.Now.Minute.ToString()))
                    {
                        DoRaffle = true;
                    }
                }
                else if (((TimeSpan)(DateTime.Now - LastRaffle)).TotalSeconds >= MinDelay)
                {
                    DoRaffle = true;
                }
                if (!RaffleRunning&&DoRaffle)
                {
                    new Thread(async () => await RaffleThread()).Start();
                }
            }
        }

        public bool RaffleRunning = false;
        public async Task RaffleThread()
        {
            RaffleRunning = true;
            int RaffleSize = 0;
            Newtonsoft.Json.Linq.JToken ChosenRaffle = null;
            foreach (Newtonsoft.Json.Linq.JToken RaffleType in BotInstance.CommandConfig["Raffle"]["Sizes"])
            {
                RaffleSize += int.Parse(RaffleType["Frequency"].ToString());
                if (RaffleSize >= RaffleNumber && ChosenRaffle==null) { ChosenRaffle = RaffleType; }
            }
            int RaffleReward = int.Parse(ChosenRaffle["Size"].ToString());
            RaffleParticipants = new List<Raffler> { };
            for (int i = 0; i < 4; i++)
            {
                String TimeLeft = (4-i)*15+" seconds";
                if (BotInstance.CommandHandler.CommandEnabled(BotInstance.CommandConfig["Raffle"], MessageType.Twitch))
                { await BotInstance.CommandHandler.SendMessage(BotInstance.CommandConfig["Raffle"]["Responses"]["LeadUp"].ToString(), BotInstance.CommandConfig["ChannelName"].ToString(), MessageType.Twitch,null, RaffleReward, -1,TimeLeft); }
                if (BotInstance.CommandHandler.CommandEnabled(BotInstance.CommandConfig["Raffle"], MessageType.Discord))
                { await BotInstance.CommandHandler.SendMessage(BotInstance.CommandConfig["Raffle"]["Responses"]["LeadUp"].ToString(), BotInstance.CommandConfig["Discord"]["NotificationChannel"].ToString(), MessageType.Discord, null, RaffleReward, -1, TimeLeft); }
                Thread.Sleep(15000);
            }
            if (RaffleParticipants.Count != 0)
            {
                int WinnerCount= int.Parse(ChosenRaffle["Winners"].ToString());
                if (WinnerCount > RaffleParticipants.Count) { WinnerCount = RaffleParticipants.Count; }
                for (int i=WinnerCount; WinnerCount > 0; WinnerCount--)
                {
                    int WinnerN = Init.Rnd.Next(0, RaffleParticipants.Count);
                    Raffler Winner = RaffleParticipants[WinnerN];
                    RaffleParticipants.RemoveAt(WinnerN);
                    if (Winner.RequestedFrom == MessageType.Twitch)
                    {
                        await BotInstance.CommandHandler.SendMessage(BotInstance.CommandConfig["Raffle"]["Responses"]["Winner"].ToString(), BotInstance.CommandConfig["ChannelName"].ToString(), MessageType.Twitch, Winner.User, RaffleReward);
                        await BotInstance.CommandHandler.SendMessage(BotInstance.CommandConfig["Raffle"]["Responses"]["OtherWinner"].ToString(), BotInstance.CommandConfig["Discord"]["NotificationChannel"].ToString(), MessageType.Discord, null, RaffleReward,-1,Winner.User.UserName);
                    }
                    else if (Winner.RequestedFrom == MessageType.Discord)
                    {
                        await BotInstance.CommandHandler.SendMessage(BotInstance.CommandConfig["Raffle"]["Responses"]["Winner"].ToString(), BotInstance.CommandConfig["Discord"]["NotificationChannel"].ToString(), MessageType.Discord, Winner.User, RaffleReward);
                        await BotInstance.CommandHandler.SendMessage(BotInstance.CommandConfig["Raffle"]["Responses"]["OtherWinner"].ToString(), BotInstance.CommandConfig["ChannelName"].ToString(), MessageType.Twitch, null, RaffleReward, -1, Winner.User.UserName);
                    }
                    Data.APIIntergrations.RewardCurrencyAPI.Objects.Bank B = Data.APIIntergrations.RewardCurrencyAPI.Objects.Bank.FromTwitchDiscord(Winner.RequestedFrom, BotInstance, Winner.User.ID);
                    Data.APIIntergrations.RewardCurrencyAPI.Objects.Bank.AdjustBalance(B, RaffleReward, "+");
                }
            }
            else
            {
                if (BotInstance.CommandHandler.CommandEnabled(BotInstance.CommandConfig["Raffle"], MessageType.Twitch))
                { await BotInstance.CommandHandler.SendMessage(BotInstance.CommandConfig["Raffle"]["Responses"]["NoOne"].ToString(), BotInstance.CommandConfig["ChannelName"].ToString(), MessageType.Twitch,null, RaffleReward); }
                if (BotInstance.CommandHandler.CommandEnabled(BotInstance.CommandConfig["Raffle"], MessageType.Discord))
                { await BotInstance.CommandHandler.SendMessage(BotInstance.CommandConfig["Raffle"]["Responses"]["NoOne"].ToString(), BotInstance.CommandConfig["Discord"]["NotificationChannel"].ToString(), MessageType.Discord, null, RaffleReward); }
            }
            RaffleNumber = (RaffleNumber + 1) % RaffleSize;
            RaffleRunning = false;
            LastRaffle = DateTime.Now;
        }

        public Dictionary<DateTime,Duel> Duels = new Dictionary<DateTime, Duel> { };
        public void RemoveDuels()
        {
            int RemoveAfter = int.Parse(BotInstance.CommandConfig["CommandSetup"]["Duel"]["CancelAfter"].ToString());
            List<DateTime> KeysToRemove = new List<DateTime> { };
            foreach (KeyValuePair<DateTime,Duel> Duel in Duels.Where(x => ((TimeSpan)(DateTime.Now - x.Key)).TotalSeconds > RemoveAfter))
            { KeysToRemove.Add(Duel.Key); }
            foreach (DateTime Key in KeysToRemove)
            { Duels.Remove(Key); }
        }

        public bool UserDueling(StandardisedUser User)
        {
            return Duels.Where(x => x.Value.Creator.ID == User.ID || x.Value.Acceptor.ID == User.ID).Count() != 0;
        }

        public Dictionary<int, DateTime> MessageHistory = new Dictionary<int, DateTime> { };
        public DateTime MessageLast = DateTime.MinValue;
        public async Task AutoMessage()
        {
            if (BotInstance.CommandHandler.LiveCheck(BotInstance.CommandConfig["AutoMessage"]))
            {
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
                            { await BotInstance.CommandHandler.SendMessage(Items[i]["Body"].ToString(),BotInstance.CommandConfig["ChannelName"].ToString(),MessageType.Twitch); }
                            if (BotInstance.CommandHandler.CommandEnabled(BotInstance.CommandConfig["AutoMessage"], MessageType.Discord))
                            { await BotInstance.CommandHandler.SendMessage(Items[i]["Body"].ToString(), BotInstance.CommandConfig["Discord"]["NotificationChannel"].ToString(), MessageType.Discord); }
                            MessageLast = DateTime.Now;
                            MessageHistory.Add(i, DateTime.Now);
                        }
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

    public class Raffler
    {
        public StandardisedUser User;
        public MessageType RequestedFrom;
    }

    public class Duel
    {
        public StandardisedMessageRequest e;
        public BotInstance BotInstance;
        public StandardisedUser Creator, Acceptor;
        public int ChangeBy;
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
