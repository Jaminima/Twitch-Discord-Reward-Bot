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
                Newtonsoft.Json.Linq.JToken JD = Request(BotInstance, "https://api.twitch.tv/helix/streams?user_login=" + BotInstance.CommandConfig["ChannelName"]);
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

        public static AccessToken GetAuthToken(BotInstance BotInstance)
        {
            if (BotInstance.AccessTokens.ContainsKey("Twitch"))
            {
                if (((TimeSpan)(BotInstance.AccessTokens["Twitch"].ExpiresAt - DateTime.Now)).TotalMinutes > 1)
                {
                    return BotInstance.AccessTokens["Twitch"];
                }
            }
            WebRequest Req = WebRequest.Create("https://id.twitch.tv/oauth2/token");
            byte[] PostData = Encoding.UTF8.GetBytes("client_id=" + BotInstance.LoginConfig["Twitch"]["API"]["ClientId"] +
                "&client_secret=" + BotInstance.LoginConfig["Twitch"]["API"]["ClientSecret"] +
                "&grant_type=refresh_token&redirect_uri=" + Init.MasterConfig["Redirect"]["WebAddress"] + "/" + Init.MasterConfig["Redirect"]["AddressPath"] + "/twitch/" + "&refresh_token=" + BotInstance.LoginConfig["Twitch"]["API"]["RefreshToken"]);
            Req.Method = "POST";
            Req.ContentType = "application/x-www-form-urlencoded";
            Req.ContentLength = PostData.Length;
            Stream PostStream = Req.GetRequestStream();
            PostStream.Write(PostData, 0, PostData.Length);
            PostStream.Flush();
            PostStream.Close();
            try
            {
                WebResponse Res = Req.GetResponse();
                string D = new StreamReader(Res.GetResponseStream()).ReadToEnd();
                Newtonsoft.Json.Linq.JObject JD = Newtonsoft.Json.Linq.JObject.Parse(D);
                BotInstance.LoginConfig["Twitch"]["API"]["RefreshToken"] = JD["refresh_token"];
                List<KeyValuePair<string, string>> Headers = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("CurrencyID", BotInstance.Currency.ID.ToString()) };
                var R = RewardCurrencyAPI.WebRequests.PostRequest("currency", Headers, true, Newtonsoft.Json.Linq.JToken.Parse("{'LoginConfig':" + BotInstance.LoginConfig.ToString() + @"}"));

                AccessToken Tk = new AccessToken(JD["access_token"].ToString(), int.Parse(JD["expires_in"].ToString()));
                if (BotInstance.AccessTokens.ContainsKey("Twitch")) { BotInstance.AccessTokens["Twitch"] = Tk; }
                else { BotInstance.AccessTokens.Add("Twitch", Tk); }

                return BotInstance.AccessTokens["Twitch"];
            }
            catch (WebException E)
            {
                Console.WriteLine(new StreamReader(E.Response.GetResponseStream()).ReadToEnd());
                return null;
            }
        }

        public static Newtonsoft.Json.Linq.JToken Request(BotInstance BotInstance,string URL,string Method="GET", string PostData=null)
        {
            try { 
                WebRequest Req = WebRequest.Create(URL);
                Req.Method = Method;
                Req.Headers.Add("Client-ID", BotInstance.LoginConfig["Twitch"]["API"]["ClientId"].ToString());
                Req.Headers.Add("Authorization", "OAuth " + GetAuthToken(BotInstance).Token);
                if (PostData != null)
                {
                    Byte[] BytePostData = Encoding.UTF8.GetBytes(PostData);
                    Req.ContentLength = BytePostData.Length;
                    Req.ContentType = "application/x-www-form-urlencoded";
                    Stream PostStream = Req.GetRequestStream();
                    PostStream.Write(BytePostData, 0, BytePostData.Length);
                    PostStream.Flush();
                    PostStream.Close();
                }
                WebResponse Res = Req.GetResponse();
                string D = new StreamReader(Res.GetResponseStream()).ReadToEnd();
                Newtonsoft.Json.Linq.JObject JD = Newtonsoft.Json.Linq.JObject.Parse(D);
                return JD;
            }
            catch (WebException E)
            {
                Console.WriteLine(new StreamReader(E.Response.GetResponseStream()).ReadToEnd());
                return null;
            }
        }

        public static Newtonsoft.Json.Linq.JToken GetChannel(BotInstance BotInstance)
        {
            return Request(BotInstance, "https://api.twitch.tv/kraken/channel");
        }

        public static Newtonsoft.Json.Linq.JToken UpdateChannelTitle(BotInstance BotInstance,string NewTitle)
        {
            return Request(BotInstance, "https://api.twitch.tv/kraken/channels/" + GetChannel(BotInstance)["display_name"], "PUT", "channel[status]="+NewTitle.Replace(" ","+"));
        }
        public static Newtonsoft.Json.Linq.JToken UpdateChannelGame(BotInstance BotInstance, string NewGame)
        {
            return Request(BotInstance, "https://api.twitch.tv/kraken/channels/" + GetChannel(BotInstance)["display_name"], "PUT", "channel[game]=" + NewGame.Replace(" ", "+"));
        }

        public static Newtonsoft.Json.Linq.JToken GetStream(BotInstance BotInstance)
        {
            return Request(BotInstance, "https://api.twitch.tv/kraken/streams/" + GetChannel(BotInstance)["display_name"].ToString());
        }

        public static Newtonsoft.Json.Linq.JToken GetViewers(BotInstance BotInstance)
        {
            WebRequest Req = WebRequest.Create("https://tmi.twitch.tv/group/user/" +BotInstance.CommandConfig["ChannelName"]+"/chatters");
            WebResponse Res = Req.GetResponse();
            string D = new StreamReader(Res.GetResponseStream()).ReadToEnd();
            Newtonsoft.Json.Linq.JObject JD = Newtonsoft.Json.Linq.JObject.Parse(D);
            return JD;
        }
    }
}
