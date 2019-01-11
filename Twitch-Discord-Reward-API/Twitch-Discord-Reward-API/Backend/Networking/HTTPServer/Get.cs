using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace Twitch_Discord_Reward_API.Backend.Networking.HTTPServer
{
    public static class Get
    {
        public static ResponseObject Handle(StandardisedRequestObject Context)
        {
            bool ErrorOccured = false;

            if (Context.Headers.AllKeys.Contains("TwitchID"))
            {
                if (!Checks.IsValidID(Context.Headers["TwitchID"])) { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, TwitchID contains invalid characters"; return Context.ResponseObject; }
            }
            if (Context.Headers.AllKeys.Contains("DiscordID"))
            {
                if (!Checks.IsValidID(Context.Headers["DiscordID"])) { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, DiscordID contains invalid characters"; return Context.ResponseObject; }
            }

            if (Context.URLSegments[1] == "bank")
            {
                if (Context.Headers.AllKeys.Contains("ID"))
                {
                    try { int.Parse(Context.Headers["ID"]); } catch { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Malformed ID"; return Context.ResponseObject; }
                    Data.Objects.Bank B = Data.Objects.Bank.FromID(int.Parse(Context.Headers["ID"]));
                    if (B != null) { Context.ResponseObject.Data = B.ToJson(); }
                    else { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, ID does not match an existing object"; ErrorOccured = true; }
                }
                else if ((Context.Headers.AllKeys.Contains("TwitchID") || Context.Headers.AllKeys.Contains("DiscordID"))&&Context.Headers.AllKeys.Contains("CurrencyID"))
                {
                    try { int.Parse(Context.Headers["CurrencyID"]); } catch { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Malformed CurrencyID"; return Context.ResponseObject; }
                    Data.Objects.Bank B = Data.Objects.Bank.FromTwitchDiscord(Context.Headers["DiscordID"], Context.Headers["TwitchID"],int.Parse(Context.Headers["CurrencyID"]));
                    if (B != null) { Context.ResponseObject.Data = B.ToJson(); }
                    else { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, TwitchID and/or DiscordID does not match an existing object"; ErrorOccured = true; }
                }
                else if (Context.Headers.AllKeys.Contains("CurrencyID"))
                {
                    try { int.Parse(Context.Headers["CurrencyID"]); } catch { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Malformed CurrencyID"; return Context.ResponseObject; }
                    List<Data.Objects.Bank> B = Data.Objects.Bank.FromCurrency(int.Parse(Context.Headers["CurrencyID"]));
                    if (B.Count != 0) { Context.ResponseObject.Data = Newtonsoft.Json.Linq.JToken.FromObject(B); }
                    else { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, CurrencyID does not match an existing object"; ErrorOccured = true; }
                }
                else if (Context.Headers.AllKeys.Contains("TwitchID") || Context.Headers.AllKeys.Contains("DiscordID"))
                {
                    List<Data.Objects.Bank> B = Data.Objects.Bank.FromTwitchDiscord(Context.Headers["DiscordID"], Context.Headers["TwitchID"]);
                    if (B.Count != 0) { Context.ResponseObject.Data = Newtonsoft.Json.Linq.JToken.FromObject(B); }
                    else { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, TwitchID and/or DiscordID does not match an existing object"; ErrorOccured = true; }
                }
                else
                {
                    ErrorOccured = true;
                    Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, No operable Headers provided";
                }
            }
            else if (Context.URLSegments[1] == "currency")
            {
                if (Context.Headers.AllKeys.Contains("ID"))
                {
                    try { int.Parse(Context.Headers["ID"]); } catch { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Malformed ID"; return Context.ResponseObject; }
                    Data.Objects.Currency C = Data.Objects.Currency.FromID(int.Parse(Context.Headers["ID"]));
                    if (Context.Headers.AllKeys.Contains("AccessToken")) {
                        Data.Objects.Login L = Data.Objects.Login.FromAccessToken(Context.Headers["AccessToken"]);
                        if ( L != null) {
                            if (Data.Objects.Currency.FromLogin(L.ID).Find(x => x.ID == C.ID) != null) { C.LoadConfigs(true); }
                        }
                    }
                    if (C != null) { Context.ResponseObject.Data = C.ToJson(); }
                    else { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, ID does not match an existing object"; ErrorOccured = true; }
                }
                else if (Context.Headers.AllKeys.Contains("LoginID"))
                {
                    try { int.Parse(Context.Headers["LoginID"]); } catch { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Malformed LoginID"; return Context.ResponseObject; }
                    List<Data.Objects.Currency> C = Data.Objects.Currency.FromLogin(int.Parse(Context.Headers["LoginID"]));
                    if (C.Count != 0) { Context.ResponseObject.Data = Newtonsoft.Json.Linq.JToken.FromObject(C); }
                    else { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, LoginID does not match an existing object"; ErrorOccured = true; }
                }
                else
                {
                    ErrorOccured = true;
                    Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, No operable Headers provided";
                }
            }
            else if (Context.URLSegments[1] == "login")
            {
                if (Context.Headers.AllKeys.Contains("ID"))
                {
                    try { int.Parse(Context.Headers["ID"]); } catch { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Malformed ID"; return Context.ResponseObject; }
                    Data.Objects.Login L = Data.Objects.Login.FromID(int.Parse(Context.Headers["ID"]));
                    if (L != null) { Context.ResponseObject.Data = L.ToJson(); }
                    else { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, ID does not match an existing object"; ErrorOccured = true; }
                }
                else if (Context.Headers.AllKeys.Contains("UserName"))
                {
                    Data.Objects.Login L = Data.Objects.Login.FromUserName(Context.Headers["UserName"]);
                    if (L != null) { Context.ResponseObject.Data = L.ToJson(); }
                    else { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, UserName does not match an existing object"; ErrorOccured = true; }
                }
                else if (Context.Headers.AllKeys.Contains("Email"))
                {
                    Data.Objects.Login L = Data.Objects.Login.FromEmail(Context.Headers["Email"]);
                    if (L != null) { Context.ResponseObject.Data = L.ToJson(); }
                    else { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Email does not match an existing object"; ErrorOccured = true; }
                }
                else if (Context.Headers.AllKeys.Contains("AccessToken"))
                {
                    Data.Objects.Login L = Data.Objects.Login.FromAccessToken(Context.Headers["AccessToken"]);
                    if (L != null) { L.HashedPassword = null; Context.ResponseObject.Data = L.ToJson(); }
                    else { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, AccessToken does not match an existing object"; ErrorOccured = true; }
                }
                else
                {
                    ErrorOccured = true;
                    Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, No operable Headers provided";
                }
            }
            else if (Context.URLSegments[1] == "bot")
            {
                if (Context.Headers.AllKeys.Contains("ID"))
                {
                    bool WithSecretData = false;
                    try { int.Parse(Context.Headers["ID"]); } catch { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Malformed ID"; return Context.ResponseObject; }
                    if (Context.Headers.AllKeys.Contains("AccessToken"))
                    {
                        Data.Objects.Login L = Data.Objects.Login.FromAccessToken(Context.Headers["AccessToken"]);
                        if (L != null)
                        {
                            if (Data.Objects.Bot.FromLogin(L.ID).Find(x => x.ID == int.Parse(Context.Headers["ID"])) != null) { WithSecretData = true; }
                        }
                    }
                    Data.Objects.Bot B = Data.Objects.Bot.FromID(int.Parse(Context.Headers["ID"]),WithSecretData);
                    if (B != null) { Context.ResponseObject.Data = B.ToJson(); }
                    else { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, ID does not match an existing object"; ErrorOccured = true; }
                }
                else if (Context.Headers.AllKeys.Contains("LoginID"))
                {
                    try { int.Parse(Context.Headers["LoginID"]); } catch { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Malformed LoginID"; return Context.ResponseObject; }
                    List<Data.Objects.Bot> B = Data.Objects.Bot.FromLogin(int.Parse(Context.Headers["LoginID"]));
                    if (B.Count != 0) { Context.ResponseObject.Data = Newtonsoft.Json.Linq.JToken.FromObject(B); }
                    else { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, LoginID does not match an existing object"; ErrorOccured = true; }
                }
                else if (Context.Headers.AllKeys.Contains("CurrencyID"))
                {
                    try { int.Parse(Context.Headers["CurrencyID"]); } catch { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Malformed CurrencyID"; return Context.ResponseObject; }
                    List<Data.Objects.Bot> B = Data.Objects.Bot.FromCurrency(int.Parse(Context.Headers["CurrencyID"]));
                    if (B.Count != 0) { Context.ResponseObject.Data = Newtonsoft.Json.Linq.JToken.FromObject(B); }
                    else { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, CurrencyID does not match an existing object"; ErrorOccured = true; }
                }
                else
                {
                    ErrorOccured = true;
                    Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, No operable Headers provided";
                }
            }
            else if (Context.URLSegments[1] == "nightbot")
            {
                if (Context.URL.Contains("code=") && Context.URL.Contains("currencyid%3d"))
                {
                    string Code = Context.URL.Split(new string[] { "code=" }, StringSplitOptions.None)[1].Split(new string[] { "&state" }, StringSplitOptions.None)[0];
                    try { int.Parse(Context.URL.Split(new string[] { "currencyid%3d" }, StringSplitOptions.None)[1] ); } catch { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Malformed CurrencyID"; return Context.ResponseObject; }
                    Data.Objects.Currency C = Data.Objects.Currency.FromID(int.Parse(Context.URL.Split(new string[] { "currencyid%3d" }, StringSplitOptions.None)[1]));
                    if (C == null) { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, CurrencyID does not match an existing object"; ErrorOccured = true; }
                    C.LoadConfigs(true);
                    WebRequest Req = WebRequest.Create("https://api.nightbot.tv/oauth2/token");
                    Req.Method = "POST";
                    byte[] PostData = Encoding.UTF8.GetBytes("client_id=" + C.LoginConfig["NightBot"]["ClientId"] +
                    "&client_secret=" + C.LoginConfig["NightBot"]["ClientSecret"] +
                    "&grant_type=authorization_code&redirect_uri="+Backend.Init.APIConfig["WebURL"]+"/nightbot/&code="+Code);
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
                        C.LoginConfig["NightBot"]["RefreshToken"] = JD["refresh_token"];
                        C.UpdateConfigs();
                    }
                    catch (WebException E)
                    {
                        ErrorOccured = true;
                        Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Something went wrong";
                        Console.WriteLine(new StreamReader(E.Response.GetResponseStream()).ReadToEnd());
                    }
                }
                else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "No Code Provided"; }
            }
            else if (Context.URLSegments[1] == "streamlabs")
            {
                string Code = Context.URL.Split(new string[] { "code=" }, StringSplitOptions.None)[1].Split(new string[] { "&state" }, StringSplitOptions.None)[0];
                try { int.Parse(Context.URL.Split(new string[] { "currencyid%3d" }, StringSplitOptions.None)[1]); } catch { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Malformed CurrencyID"; return Context.ResponseObject; }
                Data.Objects.Currency C = Data.Objects.Currency.FromID(int.Parse(Context.URL.Split(new string[] { "currencyid%3d" }, StringSplitOptions.None)[1]));
                if (C == null) { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, CurrencyID does not match an existing object"; ErrorOccured = true; }
                C.LoadConfigs(true);
                WebRequest Req = WebRequest.Create("https://streamlabs.com/api/v1.0/token");
                Req.Method = "POST";
                Req.ContentType = "application/x-www-form-urlencoded";
                byte[] PostData = Encoding.UTF8.GetBytes("grant_type=authorization_code&client_id=" + C.LoginConfig["StreamLabs"]["ClientId"] +
                    "&client_secret=" + C.LoginConfig["StreamLabs"]["ClientSecret"] +
                    "&redirect_uri="+Backend.Init.APIConfig["WebURL"]+"/streamlabs/&code="+Code);
                Req.ContentLength = PostData.Length;
                Stream PostStream = Req.GetRequestStream();
                PostStream.Write(PostData, 0, PostData.Length);
                PostStream.Flush();
                PostStream.Close();
                WebResponse Res;
                try
                {
                    Res = Req.GetResponse();
                    Newtonsoft.Json.Linq.JObject D = Newtonsoft.Json.Linq.JObject.Parse(new StreamReader(Res.GetResponseStream()).ReadToEnd());
                    C.LoginConfig["StreamLabs"]["RefreshToken"] = D["refresh_token"];
                    C.UpdateConfigs();
                }
                catch (WebException E)
                {
                    ErrorOccured = true;
                    Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Something went wrong";
                    Console.WriteLine(new StreamReader(E.Response.GetResponseStream()).ReadToEnd());
                }
            }
            else if (Context.URLSegments[1] == "twitch")
            {
                string Code = Context.URL.Split(new string[] { "code=" }, StringSplitOptions.None)[1].Split(new string[] { "&state" }, StringSplitOptions.None)[0];
                try { int.Parse(Context.URL.Split(new string[] { "currencyid%3d" }, StringSplitOptions.None)[1]); } catch { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Malformed CurrencyID"; return Context.ResponseObject; }
                Data.Objects.Currency C = Data.Objects.Currency.FromID(int.Parse(Context.URL.Split(new string[] { "currencyid%3d" }, StringSplitOptions.None)[1]));
                if (C == null) { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, CurrencyID does not match an existing object"; ErrorOccured = true; }
                C.LoadConfigs(true);
                WebRequest Req = WebRequest.Create("https://id.twitch.tv/oauth2/token");
                Req.Method = "POST";
                Req.ContentType = "application/x-www-form-urlencoded";
                byte[] PostData = Encoding.UTF8.GetBytes("grant_type=authorization_code&client_id=" + C.LoginConfig["Twitch"]["API"]["ClientId"] +
                    "&client_secret=" + C.LoginConfig["Twitch"]["API"]["ClientSecret"] +
                    "&redirect_uri=" + Backend.Init.APIConfig["WebURL"] + "/twitch/&code=" + Code);
                Req.ContentLength = PostData.Length;
                Stream PostStream = Req.GetRequestStream();
                PostStream.Write(PostData, 0, PostData.Length);
                PostStream.Flush();
                PostStream.Close();
                WebResponse Res;
                try
                {
                    Res = Req.GetResponse();
                    Newtonsoft.Json.Linq.JObject D = Newtonsoft.Json.Linq.JObject.Parse(new StreamReader(Res.GetResponseStream()).ReadToEnd());
                    C.LoginConfig["Twitch"]["API"]["RefreshToken"] = D["refresh_token"];
                    C.UpdateConfigs();
                }
                catch (WebException E)
                {
                    ErrorOccured = true;
                    Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Something went wrong";
                    Console.WriteLine(new StreamReader(E.Response.GetResponseStream()).ReadToEnd());
                }
            }
            else
            {
                Context.ResponseObject.Code = 404;
                Context.ResponseObject.Message = "Not Found";
                ErrorOccured = true;
            }
            if (ErrorOccured == false) { Context.ResponseObject.Code = 200; Context.ResponseObject.Message = "The requested task was performed successfully"; }
            return Context.ResponseObject;
        }
    }
}
