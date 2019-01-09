﻿using System;
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
                if (((TimeSpan)(BotInstance.AccessTokens["Nightbot"].ExpiresAt-DateTime.Now)).TotalMinutes>1)
                {
                    return BotInstance.AccessTokens["Nightbot"];
                }
            }
            WebRequest Req = WebRequest.Create("https://api.nightbot.tv/oauth2/token");
            byte[] PostData = Encoding.UTF8.GetBytes("client_id=" + BotInstance.LoginConfig["NightBot"]["ClientId"] +
                "&client_secret=" + BotInstance.LoginConfig["NightBot"]["ClientSecret"] +
                "&grant_type=refresh_token&redirect_uri="+Init.MasterConfig["API"]["WebAddress"] + "/" + Init.MasterConfig["API"]["AddressPath"] + "/nightbot/" + "&refresh_token=" + BotInstance.LoginConfig["NightBot"]["RefreshToken"]);
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
                List<KeyValuePair<string, string>> Headers = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string> ("CurrencyID",BotInstance.Currency.ID.ToString()) };
                var R = RewardCurrencyAPI.WebRequests.PostRequest("currency", Headers, true, Newtonsoft.Json.Linq.JToken.Parse("{'LoginConfig':" + BotInstance.LoginConfig.ToString() + @"}"));

                AccessToken Tk = new AccessToken(JD["access_token"].ToString(), int.Parse(JD["expires_in"].ToString()));
                if (BotInstance.AccessTokens.ContainsKey("Nightbot")) { BotInstance.AccessTokens["Nightbot"]=Tk; }
                else { BotInstance.AccessTokens.Add("Nightbot", Tk); }

                return BotInstance.AccessTokens["Nightbot"];
            }
            catch (WebException E)
            {
                Console.WriteLine(new StreamReader(E.Response.GetResponseStream()).ReadToEnd());
                return null;
            }
        }
    }
}
