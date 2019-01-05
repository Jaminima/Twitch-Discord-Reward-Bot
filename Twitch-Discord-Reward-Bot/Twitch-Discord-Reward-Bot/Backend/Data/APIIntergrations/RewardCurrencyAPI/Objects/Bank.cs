using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;

namespace Twitch_Discord_Reward_Bot.Backend.Data.APIIntergrations.RewardCurrencyAPI.Objects
{
    public class Bank:BaseObject
    {
        public int Balance;
        public string TwitchID, DiscordID;
        public Currency Currency;

        public static Bank FromJson(Newtonsoft.Json.Linq.JToken Json)
        {
            return Json.ToObject<Bank>();
        }

        public static Bank FromTwitchDiscord(Bots.StandardisedMessageRequest e, BotInstance BotInstance,string ID)
        {
            List<KeyValuePair<string, string>> Headers = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("CurrencyID", BotInstance.Currency.ID.ToString()) };
            if (e.MessageType == Bots.MessageType.Twitch) { Headers.Add(new KeyValuePair<string, string>("TwitchID", ID)); }
            if (e.MessageType == Bots.MessageType.Discord) { Headers.Add(new KeyValuePair<string, string>("DiscordID", ID)); }
            WebRequests.PostRequest("bank", Headers, true);
            ResponseObject RObj = WebRequests.GetRequest("bank", Headers);
            if (RObj.Code == 200)
            {
                Bank B = FromJson(RObj.Data);
                return B;
            }
            return null;
        }

        public static bool AdjustBalance(Bank Bank, int Value, string Operator)
        {
            List<KeyValuePair<string, string>> Headers = new List<KeyValuePair<string, string>> {
                new KeyValuePair<string, string>("ID", Bank.ID.ToString()),
                new KeyValuePair<string, string>("Value",Value.ToString()),
                new KeyValuePair<string, string>("Operator",Operator)
            };
            WebRequests.PostRequest("bank", Headers, true);
            ResponseObject RObj = WebRequests.GetRequest("bank", Headers);
            if (RObj.Code == 200)
            {
                return true;
            }
            return false;
        }

        public static bool MergeAccounts(Bots.StandardisedMessageRequest e,BotInstance BotInstance,string ID)
        {
            if (BotInstance.CommandConfig["Discord"]["TwitchMerging"].ToString().ToLower() == "true")
            {
                if (e.MessageType == Bots.MessageType.Discord)
                {
                    try
                    {
                        WebRequest Req = WebRequest.Create("https://discordapp.com/api/v6/users/" + ID + "/profile");
                        Req.Headers.Add("authorization", Init.MasterConfig["Discord"]["User"]["AuthToken"].ToString());
                        Req.Method = "GET";
                        WebResponse Res = Req.GetResponse();
                        string D = new StreamReader(Res.GetResponseStream()).ReadToEnd();
                        Newtonsoft.Json.Linq.JObject ProfileData = Newtonsoft.Json.Linq.JObject.Parse(D);
                        foreach (Newtonsoft.Json.Linq.JObject Connection in ProfileData["connected_accounts"])
                        {
                            if (Connection["type"].ToString() == "twitch")
                            {
                                List<KeyValuePair<string, string>> Headers = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("CurrencyID", BotInstance.Currency.ID.ToString()), new KeyValuePair<string, string>("TwitchID", Connection["id"].ToString()) };
                                ResponseObject RObj = WebRequests.GetRequest("bank", Headers);
                                if (RObj.Code == 200)
                                {
                                    Bank Twitch = FromJson(RObj.Data);
                                    Bank Discord = FromTwitchDiscord(e, BotInstance, e.SenderID);
                                    if (Twitch.DiscordID == "" && Discord.TwitchID == "")
                                    {
                                        AdjustBalance(Twitch, Twitch.Balance, "-");
                                        AdjustBalance(Discord, Twitch.Balance, "+");
                                        Headers = new List<KeyValuePair<string, string>> {
                                            new KeyValuePair<string, string>("TwitchID", Connection["id"].ToString()),
                                            new KeyValuePair<string, string>("DiscordID",ID),
                                            new KeyValuePair<string, string>("ID",Discord.ID.ToString())
                                        };
                                        RObj = WebRequests.PostRequest("bank", Headers, true);
                                        Headers = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("ID", Twitch.ID.ToString()) };
                                        RObj = WebRequests.PostRequest("bank", Headers, true);
                                    }
                                }
                                else
                                {
                                    Bank Discord = FromTwitchDiscord(e, BotInstance, e.SenderID);
                                    Headers = new List<KeyValuePair<string, string>> {
                                        new KeyValuePair<string, string>("TwitchID", Connection["id"].ToString()),
                                        new KeyValuePair<string, string>("DiscordID",ID),
                                        new KeyValuePair<string, string>("ID",Discord.ID.ToString())
                                    };
                                    RObj = WebRequests.PostRequest("bank", Headers, true);
                                }
                            }
                        }
                    }
                    catch (WebException E) { }
                }
            }
            return false;
        }
    }
}
