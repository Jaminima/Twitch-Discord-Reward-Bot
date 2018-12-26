using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Twitch_Discord_Reward_API.Backend.Data.Objects
{
    public class Bot : BaseObject
    {
        public Currency Currency;
        public string AccessToken, RefreshToken;
        public DateTime TokenRefreshDateTime;
        public Login OwnerLogin;

        public Bot FromJson(Newtonsoft.Json.Linq.JToken Json)
        {
            return Json.ToObject<Bot>();
        }
    }
}
