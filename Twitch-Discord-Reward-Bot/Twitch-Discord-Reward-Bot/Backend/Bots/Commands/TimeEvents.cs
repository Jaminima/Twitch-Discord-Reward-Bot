using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;

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

        async Task TimeThread()
        {
            while (true){
                await Fish();
                await AutoMessage();
                RemoveDuels();
                PerformRaffle();
                RewardForViewing();
                await LiveNotifications();
                await CheckForDonations();
                System.Threading.Thread.Sleep(10000);
            }
        }

        bool IsLive = false;
        async Task LiveNotifications()
        {
            if (BotInstance.CommandConfig["LiveNotifications"]["Enabled"].ToString() == "true") { 
            bool NewIsLive = Data.APIIntergrations.Twitch.IsLive(BotInstance);
                if (IsLive != NewIsLive && NewIsLive)
                {
                    foreach (Data.APIIntergrations.RewardCurrencyAPI.Objects.Viewer Viewer in Data.APIIntergrations.RewardCurrencyAPI.Objects.Viewer.FromCurrency(BotInstance))
                    {
                        if (Viewer.LiveNotifcations)
                        {
                            if (Viewer.DiscordID != null)
                            {
                                await BotInstance.DiscordBot.Client.GetUser(ulong.Parse(Viewer.DiscordID)).SendMessageAsync(BotInstance.CommandHandler.MessageParser(BotInstance.CommandConfig["LiveNotifications"]["Responses"]["Live"].ToString(), null, MessageType.Discord));
                            }
                        }
                    }
                }
            }
        }

        DateTime LastDonationCheck = DateTime.MinValue;
        async Task CheckForDonations()
        {
            if (((TimeSpan)(DateTime.Now - LastDonationCheck)).TotalSeconds < 60) { return; }
            LastDonationCheck = DateTime.Now;
            Newtonsoft.Json.Linq.JToken NetData = Data.APIIntergrations.Streamlabs.GetDonations(BotInstance),
                LocalData=Data.FileHandler.ReadJSON("./Data/DonationCache/"+BotInstance.Currency.ID+".json");
            int DonationReward = int.Parse(BotInstance.CommandConfig["AutoRewards"]["Donating"]["RewardPerWhole"].ToString());
            if (LocalData != null)
            {
                if (NetData["data"][0]["donation_id"].ToString() != LocalData["data"][0]["donation_id"].ToString())
                {
                    for (int i = 0; i < LocalData["data"].Count(); i++)
                    {
                        if (LocalData["data"][0]["donation_id"].ToString() != NetData["data"][i]["donation_id"].ToString())
                        {
                            Newtonsoft.Json.Linq.JToken Donation = NetData["data"][i];
                            await RewardDonator(Donation,DonationReward);
                        }
                    }
                    Data.FileHandler.SaveJSON("./Data/DonationCache/" + BotInstance.Currency.ID + ".json", NetData);
                }
            }
            else
            {
                foreach (Newtonsoft.Json.Linq.JToken Donation in NetData["data"])
                {
                    await RewardDonator(Donation,DonationReward);
                }
                Data.FileHandler.SaveJSON("./Data/DonationCache/" + BotInstance.Currency.ID + ".json",NetData);
            }
        }
        async Task RewardDonator(Newtonsoft.Json.Linq.JToken Donation,int DonationReward)
        {
            int DonationAmount = (int)Math.Round(double.Parse(Donation["amount"].ToString()), 2),
                AdjustedReward= (int)Math.Ceiling((double)DonationAmount * DonationReward);
            StandardisedUser S = StandardisedUser.FromTwitchUsername(Donation["name"].ToString(), BotInstance);
            if (S != null)
            {
                Data.APIIntergrations.RewardCurrencyAPI.Objects.Viewer B = Data.APIIntergrations.RewardCurrencyAPI.Objects.Viewer.FromTwitchDiscord(MessageType.Twitch, BotInstance, S.ID);
                Data.APIIntergrations.RewardCurrencyAPI.Objects.Viewer.AdjustBalance(B, DonationAmount, "+");
                await BotInstance.CommandHandler.SendMessage(BotInstance.CommandConfig["AutoRewards"]["Donating"]["Response"].ToString(),
                    BotInstance.CommandConfig["ChannelName"].ToString(), MessageType.Twitch, S, AdjustedReward,-1,
                    DonationAmount+" "+Donation["currency"].ToString());
            }
        }

        public List<Viewer> ViewerRewardTracking = new List<Viewer> { };
        public DateTime LastViewerRewardCheck = DateTime.MinValue;
        void RewardForViewing()
        {
            if (((TimeSpan)(DateTime.Now - LastViewerRewardCheck)).TotalSeconds < 60) { return; }
            LastViewerRewardCheck = DateTime.Now;
            Newtonsoft.Json.Linq.JToken JData = Data.APIIntergrations.Twitch.GetViewers(BotInstance);
            int Reward = int.Parse(BotInstance.CommandConfig["AutoRewards"]["Viewing"]["RewardPerMinute"].ToString());
            IEnumerable<Newtonsoft.Json.Linq.JToken> Merged = JData["chatters"]["vips"].
                Union(JData["chatters"]["moderators"]).
                Union(JData["chatters"]["staff"]).
                Union(JData["chatters"]["admins"]).
                Union(JData["chatters"]["global_mods"]).
                Union(JData["chatters"]["viewers"]);
            foreach (Newtonsoft.Json.Linq.JToken StreamViewer in Merged)
            {
                StandardisedUser U = StandardisedUser.FromTwitchUsername(StreamViewer.ToString(), BotInstance);
                Data.APIIntergrations.RewardCurrencyAPI.Objects.Viewer Viewer = Data.APIIntergrations.RewardCurrencyAPI.Objects.Viewer.FromTwitchDiscord(MessageType.Twitch,BotInstance,U.ID);
                Viewer.WatchTime += 1;
                List<KeyValuePair<string, string>> Headers = new List<KeyValuePair<string, string>> {
                    new KeyValuePair<string, string>("ID",Viewer.ID.ToString()),
                    new KeyValuePair<string, string>("WatchTime",Viewer.WatchTime.ToString())
                };
                Data.APIIntergrations.RewardCurrencyAPI.WebRequests.PostRequest("viewer", Headers, true);
                RewardUser(Reward, U, MessageType.Twitch);
            }
        }
        void RewardUser(int Reward,StandardisedUser U,MessageType MessageType)
        {
            Data.APIIntergrations.RewardCurrencyAPI.Objects.Viewer B = Data.APIIntergrations.RewardCurrencyAPI.Objects.Viewer.FromTwitchDiscord(MessageType, BotInstance, U.ID);
            Data.APIIntergrations.RewardCurrencyAPI.Objects.Viewer.AdjustBalance(B, Reward, "+");
        }

        int RaffleNumber=0;
        public List<Raffler> RaffleParticipants = new List<Raffler> { };
        DateTime LastRaffle = DateTime.MinValue;
        public bool UserRaffleing(StandardisedUser User)
        {
            return RaffleParticipants.Where(x=>x.User.ID==User.ID).Count()!=0;
        }
        void PerformRaffle()
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

        bool RaffleRunning = false;
        async Task RaffleThread()
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
                    Data.APIIntergrations.RewardCurrencyAPI.Objects.Viewer B = Data.APIIntergrations.RewardCurrencyAPI.Objects.Viewer.FromTwitchDiscord(Winner.RequestedFrom, BotInstance, Winner.User.ID);
                    Data.APIIntergrations.RewardCurrencyAPI.Objects.Viewer.AdjustBalance(B, RaffleReward, "+");
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

        Dictionary<int, DateTime> MessageHistory = new Dictionary<int, DateTime> { };
        DateTime MessageLast = DateTime.MinValue;
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
                Data.APIIntergrations.RewardCurrencyAPI.Objects.Viewer B = Data.APIIntergrations.RewardCurrencyAPI.Objects.Viewer.FromTwitchDiscord(Fisher.Value.e, Fisher.Value.BotInstance, Fisher.Value.e.SenderID);
                if (Data.APIIntergrations.RewardCurrencyAPI.Objects.Viewer.AdjustBalance(B, int.Parse(ChosenItem["Reward"].ToString()), "+"))
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

    public class Viewer
    {
        public StandardisedUser User;
        public DateTime LastDiscordMessage = DateTime.MinValue, 
            LastTwitchMessage = DateTime.MinValue;
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
