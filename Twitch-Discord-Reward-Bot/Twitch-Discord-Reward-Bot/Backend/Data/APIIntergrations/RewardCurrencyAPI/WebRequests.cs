using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;

namespace Twitch_Discord_Reward_Bot.Backend.Data.APIIntergrations.RewardCurrencyAPI
{
    public static class WebRequests
    {
        public static ResponseObject GetRequest(string URL, List<KeyValuePair<string, string>> Headers = null)
        {
            WebRequest Req = WebRequest.Create(Init.MasterConfig["API"]["WebAddress"] + ":" + Init.MasterConfig["API"]["Port"] + "/" + URL);
            Req.Method = "GET";
            if (Headers != null)
            {
                foreach (KeyValuePair<string, string> HeaderPair in Headers)
                {
                    Req.Headers.Add(HeaderPair.Key, HeaderPair.Value);
                }
            }
            try
            {
                WebResponse Res = Req.GetResponse();
                string StreamString = new StreamReader(Res.GetResponseStream()).ReadToEnd();
                Newtonsoft.Json.Linq.JToken JData = Newtonsoft.Json.Linq.JToken.Parse(StreamString);
                ResponseObject RObj = ResponseObject.FromJson(JData);
                return RObj;
            }
            catch (WebException E)
            {
                Console.WriteLine(E);
                return null;
            }
        }

        static string LastAuthToken = null;
        static DateTime LastRefreshed = DateTime.MinValue;
        public static string GetAuthToken()
        {
            if (((TimeSpan)(DateTime.Now - LastRefreshed)).TotalSeconds > 600)
            {
                WebRequest Req = WebRequest.Create(Init.MasterConfig["API"]["WebAddress"] + ":" + Init.MasterConfig["API"]["Port"] + "/bot");
                Req.Headers.Add("RefreshToken", Init.MasterConfig["API"]["RefreshToken"].ToString());
                Req.Method = "POST";
                Stream PostStream = Req.GetRequestStream();
                PostStream.Write(new byte[] { }, 0, new byte[] { }.Length);
                PostStream.Flush();
                PostStream.Close();
                try
                {
                    WebResponse Res = Req.GetResponse();
                    string StreamString = new StreamReader(Res.GetResponseStream()).ReadToEnd();
                    Newtonsoft.Json.Linq.JToken JData = Newtonsoft.Json.Linq.JToken.Parse(StreamString);
                    ResponseObject RObj = ResponseObject.FromJson(JData);
                    if (RObj.Data.Count()!=0)
                    {
                        Objects.Bot B = Objects.Bot.FromJson(RObj.Data);
                        Init.MasterConfig["API"]["RefreshToken"] = B.RefreshToken;
                        LastRefreshed = B.TokenRefreshDateTime;
                        LastAuthToken = B.AccessToken;
                        FileHandler.SaveJSON("./Data/Master.config.json", Init.MasterConfig);
                    }
                    else { return null; }
                }
                catch (WebException E)
                {
                    Console.WriteLine(E);
                    return null;
                }
            }
            return LastAuthToken;
        }
    }
}
