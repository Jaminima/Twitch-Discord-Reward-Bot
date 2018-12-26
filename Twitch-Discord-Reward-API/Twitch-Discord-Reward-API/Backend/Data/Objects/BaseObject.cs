using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Twitch_Discord_Reward_API.Backend.Data.Objects
{
    public class BaseObject
    {
        public int ID;

        public Newtonsoft.Json.Linq.JToken ToJson()
        {
            return Newtonsoft.Json.Linq.JToken.FromObject(this);
        }
    }
}
