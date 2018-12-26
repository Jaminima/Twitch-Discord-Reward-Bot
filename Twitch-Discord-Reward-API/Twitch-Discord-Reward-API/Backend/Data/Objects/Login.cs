using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Twitch_Discord_Reward_API.Backend.Data.Objects
{
    public class Login : BaseObject
    {
        public string UserName, HashedPassword, AccessToken;
        public DateTime LastLoginDateTime;

        public Login FromJson(Newtonsoft.Json.Linq.JToken Json)
        {
            return Json.ToObject<Login>();
        }
    }
}
