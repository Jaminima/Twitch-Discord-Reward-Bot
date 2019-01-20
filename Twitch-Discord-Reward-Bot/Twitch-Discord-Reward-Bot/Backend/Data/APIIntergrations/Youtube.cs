using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;

namespace Twitch_Discord_Reward_Bot.Backend.Data.APIIntergrations
{
    public static class Youtube
    {
        public static string LatestVid(BotInstance BotInstance)
        {
            WebRequest Req = WebRequest.Create("https://www.googleapis.com/youtube/v3/search?key=" + BotInstance.LoginConfig["Youtube"]["AuthToken"].ToString()
                + "&channelId=" + BotInstance.LoginConfig["Youtube"]["ChannelID"].ToString()
                + "&part=snippet,id&order=date&maxResults=1");
            Req.Method = "GET";
            try
            {
                WebResponse Res = Req.GetResponse();
                string SData = new StreamReader(Res.GetResponseStream()).ReadToEnd();
                Newtonsoft.Json.Linq.JToken Resp = Newtonsoft.Json.Linq.JToken.Parse(SData);
                return "https://youtu.be/" + Resp["items"][0]["id"]["videoId"].ToString();
            }
            catch { return null; }
        }
    }
}
