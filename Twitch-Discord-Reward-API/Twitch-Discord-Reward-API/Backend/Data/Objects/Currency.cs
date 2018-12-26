using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Twitch_Discord_Reward_API.Backend.Data.Objects
{
    public class Currency : BaseObject
    {
        public Login OwnerLogin;

        public Currency FromJson(Newtonsoft.Json.Linq.JToken Json)
        {
            return Json.ToObject<Currency>();
        }
    }
}
