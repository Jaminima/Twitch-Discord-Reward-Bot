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

            if (ErrorOccured == false) { Context.ResponseObject.Code = 200; Context.ResponseObject.Message = "The requested task was performed successfully"; }
            return Context.ResponseObject;
        }

        static bool AuthCheck(StandardisedRequestObject Context,int CurrencyID=-1)
        {
            if (Context.Headers.AllKeys.Contains("AuthToken"))
            {
                if (CurrencyID != -1) { return Backend.Data.Objects.Bot.IsValidAccessToken(Context.Headers["AuthToken"], CurrencyID); }
                else { return Backend.Data.Objects.Bot.IsValidAccessToken(Context.Headers["AuthToken"]); }
            }
            else { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, AuthToken is missing"; return false; }
        }
    }
}
