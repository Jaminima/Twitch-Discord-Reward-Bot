using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Twitch_Discord_Reward_API.Backend.Data.Objects
{
    public class BaseObject
    {
        public int ID;//All objects will have an ID value

        public Newtonsoft.Json.Linq.JToken ToJson()//All objects will need to be convertable into json format for transmission
        {
            return Newtonsoft.Json.Linq.JToken.FromObject(this);
        }
    }
}
