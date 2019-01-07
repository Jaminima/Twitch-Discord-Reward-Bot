using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;

namespace Twitch_Discord_Reward_Bot.Backend.Data.APIIntergrations
{
    public static class Twitch
    {
        static bool Islive = false;
        static DateTime LastLiveCheck;
        public static bool IsLive(BotInstance BotInstance)
        {
            if (((TimeSpan)(DateTime.Now - LastLiveCheck)).TotalSeconds < 15) { return Islive; }
            try
            {
                WebRequest Req = WebRequest.Create("https://api.twitch.tv/helix/streams?user_login=" + BotInstance.CommandConfig["ChannelName"]);
                Req.Method = "GET";
                Req.Headers.Add("Client-ID", BotInstance.LoginConfig["Twitch"]["API"]["ClientId"].ToString());
                Req.Headers.Add("Authorization", "OAuth " + BotInstance.LoginConfig["Twitch"]["API"]["AccessToken"].ToString());
                WebResponse Res = Req.GetResponse();
                string D = new StreamReader(Res.GetResponseStream()).ReadToEnd();
                Newtonsoft.Json.Linq.JObject JD = Newtonsoft.Json.Linq.JObject.Parse(D);
                LastLiveCheck = DateTime.Now;
                if (JD["data"].Count() != 0)
                {
                    if (JD["data"][0]["type"].ToString() == "live")
                    {
                        Islive = true;
                        return true;
                    }
                }
                Islive = false;
                return false;
            }
            catch (Exception E)
            {
                Console.WriteLine(E);
                return false;
            }
        }
    }
}
