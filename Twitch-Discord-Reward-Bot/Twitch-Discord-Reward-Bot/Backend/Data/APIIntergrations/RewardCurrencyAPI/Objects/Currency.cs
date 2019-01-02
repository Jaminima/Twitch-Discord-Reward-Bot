using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Twitch_Discord_Reward_Bot.Backend.Data.APIIntergrations.RewardCurrencyAPI.Objects
{
    public class Currency : BaseObject
    {
        public Login OwnerLogin;
        public Newtonsoft.Json.Linq.JToken LoginConfig, CommandConfig;

        public static Currency FromJson(Newtonsoft.Json.Linq.JToken Json)
        {
            return Json.ToObject<Currency>();
        }
    }
}
