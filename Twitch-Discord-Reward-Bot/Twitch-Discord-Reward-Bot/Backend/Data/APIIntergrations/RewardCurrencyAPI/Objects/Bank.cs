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
    }
}
