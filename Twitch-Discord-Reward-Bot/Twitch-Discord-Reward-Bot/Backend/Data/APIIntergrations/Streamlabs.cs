using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;

namespace Twitch_Discord_Reward_Bot.Backend.Data.APIIntergrations
{
    public static class Streamlabs
    {
        public static AccessToken GetAuthToken(BotInstance BotInstance)
        {
            if (BotInstance.AccessTokens.ContainsKey("Streamlabs"))
            {
                if (((TimeSpan)(BotInstance.AccessTokens["Streamlabs"].ExpiresAt - DateTime.Now)).TotalMinutes > 1)
                {
                    return BotInstance.AccessTokens["Streamlabs"];
                }
            }
            WebRequest Req = WebRequest.Create("https://streamlabs.com/api/v1.0/token");
            byte[] PostData = Encoding.UTF8.GetBytes("client_id=" + BotInstance.LoginConfig["StreamLabs"]["ClientId"] +
                "&client_secret=" + BotInstance.LoginConfig["StreamLabs"]["ClientSecret"] +
                "&grant_type=refresh_token&redirect_uri=" + Init.MasterConfig["Redirect"]["WebAddress"] + "/" + Init.MasterConfig["Redirect"]["AddressPath"] + "/streamlabs/" + "&refresh_token=" + BotInstance.LoginConfig["StreamLabs"]["RefreshToken"]);
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
                BotInstance.LoginConfig["StreamLabs"]["RefreshToken"] = JD["refresh_token"];
                List<KeyValuePair<string, string>> Headers = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("CurrencyID", BotInstance.Currency.ID.ToString()) };
                var R = RewardCurrencyAPI.WebRequests.PostRequest("currency", Headers, true, Newtonsoft.Json.Linq.JToken.Parse("{'LoginConfig':" + BotInstance.LoginConfig.ToString() + @"}"));

                AccessToken Tk = new AccessToken(JD["access_token"].ToString(), int.Parse(JD["expires_in"].ToString()));
                if (BotInstance.AccessTokens.ContainsKey("Streamlabs")) { BotInstance.AccessTokens["Streamlabs"] = Tk; }
                else { BotInstance.AccessTokens.Add("Streamlabs", Tk); }

                return BotInstance.AccessTokens["Streamlabs"];
            }
            catch (WebException E)
            {
                Console.WriteLine(new StreamReader(E.Response.GetResponseStream()).ReadToEnd());
                return null;
            }
        }

        public static Newtonsoft.Json.Linq.JToken GenericExecute(BotInstance BotInstance, string URL, string Data="", string Method="GET",bool URLAuth=false)
        {
            if (URLAuth)
            {
                if (URL.Contains("?")) { URL += "&access_token=" + GetAuthToken(BotInstance).Token; }
                else { URL += "?access_token=" + GetAuthToken(BotInstance).Token; }
            }
            WebRequest Req = WebRequest.Create(URL);
            Req.Method = Method;
            Req.ContentType = "application/x-www-form-urlencoded";
            //Req.Timeout = 2000;
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

        public static Newtonsoft.Json.Linq.JToken GetDonations(BotInstance BotInstance)
        {
            return GenericExecute(BotInstance, "https://streamlabs.com/api/v1.0/donations?limit=100",URLAuth:true);
        }

        public static Newtonsoft.Json.Linq.JToken PlayAlert(BotInstance BotInstance, string SoundURL)
        {
            return GenericExecute(BotInstance, "https://streamlabs.com/api/v1.0/alerts", "access_token="+ GetAuthToken(BotInstance).Token + "&type=merch&duration=0&message= &image_href=https://upload.wikimedia.org/wikipedia/commons/thumb/0/0b/TransparentPlaceholder.svg/240px-TransparentPlaceholder.svg.png&sound_href=" + SoundURL, Method:"POST");
        }
    }
}
