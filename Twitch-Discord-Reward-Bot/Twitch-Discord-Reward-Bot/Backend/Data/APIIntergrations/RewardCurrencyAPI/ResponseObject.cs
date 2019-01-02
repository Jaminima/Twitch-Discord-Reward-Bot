using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Twitch_Discord_Reward_Bot.Backend.Data.APIIntergrations.RewardCurrencyAPI
{

    public class ResponseObject:Objects.BaseObject
    {
        public Newtonsoft.Json.Linq.JToken Data;
        public int Code;
        public string Message;

        public static ResponseObject FromJson(Newtonsoft.Json.Linq.JToken Json)
        {
            return Json.ToObject<ResponseObject>();
        }
    }
}
