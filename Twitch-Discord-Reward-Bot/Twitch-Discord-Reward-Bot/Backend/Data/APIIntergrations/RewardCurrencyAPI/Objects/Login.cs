using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Twitch_Discord_Reward_Bot.Backend.Data.APIIntergrations.RewardCurrencyAPI.Objects
{
    public class Login : BaseObject
    {
        public string UserName, HashedPassword, AccessToken, Email;
        public DateTime LastLoginDateTime;

        public static Login FromJson(Newtonsoft.Json.Linq.JToken Json)
        {
            return Json.ToObject<Login>();
        }
    }
}
