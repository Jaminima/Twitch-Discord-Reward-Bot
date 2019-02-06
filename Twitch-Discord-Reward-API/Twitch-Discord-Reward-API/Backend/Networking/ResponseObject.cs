using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Twitch_Discord_Reward_API.Backend.Networking
{
    public class ResponseObject//This object stores the data that will be returned to the requestor
    {
        public Newtonsoft.Json.Linq.JToken Data;//This will store the json, for the data that will be returned to the requestor
        public int Code;//These are used in place of a code and error message in the response, to seperate errors from the backend data handling and errors with the networking
        public string Message;

        public Newtonsoft.Json.Linq.JToken ToJson()//Allows us to convert this object to json form, for transmission
        {
            return Newtonsoft.Json.Linq.JToken.FromObject(this);
        }
    }
}
