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
            Backend.Data.Objects.Bot CorrespondingBot = AuthCheck(Context);
            if (Context.URLSegments[1] == "bank")
            {
                if (Context.Headers.AllKeys.Contains("TwitchID") || Context.Headers.AllKeys.Contains("DiscordID"))
                {
                    if (CorrespondingBot != null)
                    {
                        Data.Objects.Bank B = new Data.Objects.Bank();
                        B.DiscordID = Context.Headers["DiscordID"];
                        B.TwitchID = Context.Headers["TwitchID"];
                        B.Currency = CorrespondingBot.Currency;
                        B.Balance = int.Parse(CorrespondingBot.Currency.CommandConfig["InititalBalance"].ToString());
                        B.Save();
                    }
                    else
                    {
                        ErrorOccured = true;
                        Context.ResponseObject.Code = 403; Context.ResponseObject.Message = "Invalid AuthToken";
                    }
                }
                else
                {
                    ErrorOccured = true;
                    Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, No operable Headers provided";
                }
            }
            if (ErrorOccured == false) { Context.ResponseObject.Code = 200; Context.ResponseObject.Message = "The requested task was performed successfully"; }
            return Context.ResponseObject;
        }

        static Data.Objects.Bot AuthCheck(StandardisedRequestObject Context)
        {
            if (Context.Headers.AllKeys.Contains("AuthToken"))
            {
                return Data.Objects.Bot.FromAccessToken(Context.Headers["AuthToken"]);
            }
            else { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, AuthToken is missing"; return null; }
        }
    }
}
