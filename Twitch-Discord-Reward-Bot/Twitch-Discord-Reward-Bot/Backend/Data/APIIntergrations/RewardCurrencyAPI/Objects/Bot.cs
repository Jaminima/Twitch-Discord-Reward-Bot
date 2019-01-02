using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Twitch_Discord_Reward_Bot.Backend.Data.APIIntergrations.RewardCurrencyAPI.Objects
{
    public class Bot : BaseObject
    {
        public string InviteCode;
        public Currency Currency;
        public string AccessToken, RefreshToken;
        public DateTime TokenRefreshDateTime;
        public Login OwnerLogin;

        public static Bot FromJson(Newtonsoft.Json.Linq.JToken Json)
        {
            return Json.ToObject<Bot>();
        }
    }
}
