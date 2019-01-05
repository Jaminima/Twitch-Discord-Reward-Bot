using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Twitch_Discord_Reward_Bot.Backend.Data.APIIntergrations.RewardCurrencyAPI.Objects
{
    public class Bank:BaseObject
    {
        public int Balance;
        public string TwitchID, DiscordID;
        public Currency Currency;

        public static Bank FromJson(Newtonsoft.Json.Linq.JToken Json)
        {
            return Json.ToObject<Bank>();
        }

        public static Bank FromTwitchDiscord(Bots.StandardisedMessageRequest e, BotInstance BotInstance,string ID)
        {
            List<KeyValuePair<string, string>> Headers = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("CurrencyID", BotInstance.Currency.ID.ToString()) };
            if (e.MessageType == Bots.MessageType.Twitch) { Headers.Add(new KeyValuePair<string, string>("TwitchID", ID)); }
            if (e.MessageType == Bots.MessageType.Discord) { Headers.Add(new KeyValuePair<string, string>("DiscordID", ID)); }
            WebRequests.PostRequest("bank", Headers, true);
            ResponseObject RObj = WebRequests.GetRequest("bank", Headers);
            if (RObj.Code == 200)
            {
                Bank B = FromJson(RObj.Data);
                return B;
            }
            return null;
        }

        public static bool AdjustBalance(Bank Bank, int Value, string Operator)
        {
            List<KeyValuePair<string, string>> Headers = new List<KeyValuePair<string, string>> {
                new KeyValuePair<string, string>("ID", Bank.ID.ToString()),
                new KeyValuePair<string, string>("Value",Value.ToString()),
                new KeyValuePair<string, string>("Operator",Operator)
            };
            WebRequests.PostRequest("bank", Headers, true);
            ResponseObject RObj = WebRequests.GetRequest("bank", Headers);
            if (RObj.Code == 200)
            {
                return true;
            }
            return false;
        }
    }
}
