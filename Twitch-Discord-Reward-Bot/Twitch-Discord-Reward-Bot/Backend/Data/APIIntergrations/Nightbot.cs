using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;

namespace Twitch_Discord_Reward_Bot.Backend.Data.APIIntergrations
{
    public static class Nightbot
    {
        public static AccessToken GetAuthToken(BotInstance BotInstance)
        {
            if (BotInstance.AccessTokens.ContainsKey("Nightbot"))
            {
                if (((TimeSpan)(BotInstance.AccessTokens["Nightbot"].ExpiresAt - DateTime.Now)).TotalMinutes > 1)
                {
                    return BotInstance.AccessTokens["Nightbot"];
                }
            }
            WebRequest Req = WebRequest.Create("https://api.nightbot.tv/oauth2/token");
            byte[] PostData = Encoding.UTF8.GetBytes("client_id=" + BotInstance.LoginConfig["NightBot"]["ClientId"] +
                "&client_secret=" + BotInstance.LoginConfig["NightBot"]["ClientSecret"] +
                "&grant_type=refresh_token&redirect_uri=" + Init.MasterConfig["Redirect"]["WebAddress"] + "/" + Init.MasterConfig["Redirect"]["AddressPath"] + "/nightbot/" + "&refresh_token=" + BotInstance.LoginConfig["NightBot"]["RefreshToken"]);
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
                BotInstance.LoginConfig["NightBot"]["RefreshToken"] = JD["refresh_token"];
                List<KeyValuePair<string, string>> Headers = new List<KeyValuePair<string, string>> {
                    new KeyValuePair<string, string>("CurrencyID", BotInstance.Currency.ID.ToString())
                };
                var R = RewardCurrencyAPI.WebRequests.PostRequest("currency", Headers, true, Newtonsoft.Json.Linq.JToken.Parse("{'LoginConfig':" + BotInstance.LoginConfig.ToString() + @"}"));

                AccessToken Tk = new AccessToken(JD["access_token"].ToString(), int.Parse(JD["expires_in"].ToString()));
                if (BotInstance.AccessTokens.ContainsKey("Nightbot")) { BotInstance.AccessTokens["Nightbot"] = Tk; }
                else { BotInstance.AccessTokens.Add("Nightbot", Tk); }

                return BotInstance.AccessTokens["Nightbot"];
            }
            catch (WebException E)
            {
                Console.WriteLine(new StreamReader(E.Response.GetResponseStream()).ReadToEnd());
                return null;
            }
        }

        public static Newtonsoft.Json.Linq.JToken GenericExecute(BotInstance BotInstance, string URL, string Method)
        {
            return GenericExecute(BotInstance, URL, "", Method);
        }

        public static Newtonsoft.Json.Linq.JToken GenericExecute(BotInstance BotInstance, string URL, string Data, string Method)
        {
            WebRequest Req = WebRequest.Create(URL);
            Req.Method = Method;
            Req.Headers.Add("Authorization", "Bearer " + GetAuthToken(BotInstance).Token);
            Req.ContentType = "application/x-www-form-urlencoded";
            if (Data != "")
            {
                byte[] PostData = Encoding.UTF8.GetBytes(Data);
                Req.ContentLength = PostData.Length;
                Stream PostStream = Req.GetRequestStream();
                PostStream.Write(PostData, 0, PostData.Length);
                PostStream.Flush();
                PostStream.Close();
            }
            try
            {
                WebResponse Res = Req.GetResponse();
                string D = new StreamReader(Res.GetResponseStream()).ReadToEnd();
                Newtonsoft.Json.Linq.JObject JD = Newtonsoft.Json.Linq.JObject.Parse(D);
                return JD;
            }
            catch (WebException E)
            {
                return Newtonsoft.Json.Linq.JToken.Parse(new StreamReader(E.Response.GetResponseStream()).ReadToEnd());
            }
        }
        static int PrevVolume = 10;

        public static Newtonsoft.Json.Linq.JToken GetQueue(BotInstance BotInstance)
        {
            return GenericExecute(BotInstance, "https://api.nightbot.tv/1/song_requests/queue", "GET");
        }

        public static Newtonsoft.Json.Linq.JToken PauseSong(BotInstance BotInstance)
        {
            PrevVolume = int.Parse(GetQueue(BotInstance)["settings"]["volume"].ToString());
            return GenericExecute(BotInstance, "https://api.nightbot.tv/1/song_requests", "volume=0", "PUT");
        }

        public static Newtonsoft.Json.Linq.JToken PlaySong(BotInstance BotInstance)
        {
            return GenericExecute(BotInstance, "https://api.nightbot.tv/1/song_requests", "volume=" + PrevVolume, "PUT");
        }

        public static Newtonsoft.Json.Linq.JToken SkipSong(BotInstance BotInstance)
        {
            return GenericExecute(BotInstance, "https://api.nightbot.tv/1/song_requests/queue/skip", "POST");
        }

        public static Newtonsoft.Json.Linq.JToken SetVolume(BotInstance BotInstance, int Volume)
        {
            return GenericExecute(BotInstance, "https://api.nightbot.tv/1/song_requests", "volume=" + Volume, "PUT");
        }

        public static Newtonsoft.Json.Linq.JToken RequestSong(BotInstance BotInstance, string Url)
        {
            return GenericExecute(BotInstance, "https://api.nightbot.tv/1/song_requests/queue", "q=" + Url, "POST");
        }

        public static Newtonsoft.Json.Linq.JToken RemoveItem(BotInstance BotInstance, int i)
        {
            Newtonsoft.Json.Linq.JToken Song = GetSongFromPos(BotInstance, i);
            return RemoveID(BotInstance, Song["_id"].ToString());
        }

        public static Newtonsoft.Json.Linq.JToken RemoveID(BotInstance BotInstance, string ID)
        {
            return GenericExecute(BotInstance, "https://api.nightbot.tv/1/song_requests/queue/" + ID, "DELETE");
        }

        public static Newtonsoft.Json.Linq.JToken PromoteItem(BotInstance BotInstance, int i)
        {
            Newtonsoft.Json.Linq.JToken Song = GetSongFromPos(BotInstance, i);
            return RemoveID(BotInstance, Song["_id"].ToString());
        }

        public static Newtonsoft.Json.Linq.JToken PromoteID(BotInstance BotInstance, string ID)
        {
            return GenericExecute(BotInstance, "https://api.nightbot.tv/1/song_requests/queue/" + ID + "/promote", "POST");
        }

        public static Newtonsoft.Json.Linq.JToken GetSongFromPos(BotInstance BotInstance, int i)
        {
            Newtonsoft.Json.Linq.JToken CurrentQueue = GenericExecute(BotInstance, "https://api.nightbot.tv/1/song_requests/queue", "GET");
            if (CurrentQueue["status"].ToString() != "200") { return Newtonsoft.Json.Linq.JToken.Parse("{\"message\":\"Error occured\",\"status\":400}"); }
            if (CurrentQueue["queue"].Count() < i || i <= 0) { return Newtonsoft.Json.Linq.JToken.Parse("{\"message\":\"Out of range!\",\"status\":400}"); }
            return CurrentQueue["queue"][i - 1];
        }
    }
}
