using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Twitch_Discord_Reward_API.Backend.Networking.HTTPServer
{
    public  static class Post
    {
        public static ResponseObject Handle(StandardisedRequestObject Context)
        {
            bool ErrorOccured = false;
            if (Context.URLSegments[1] == "user")
            {

            }
            if (ErrorOccured == false) { Context.ResponseObject.Code = 200; Context.ResponseObject.Message = "The requested task was performed successfully"; }
            return Context.ResponseObject;
        }
    }
}
