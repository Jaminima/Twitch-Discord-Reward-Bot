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

            // Check if TwitchID and DiscordID only compose of numbers
            if (Context.Headers.AllKeys.Contains("TwitchID"))
            {
                if (!Checks.IsValidID(Context.Headers["TwitchID"])) { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, TwitchID contains invalid characters"; return Context.ResponseObject; }
            }
            if (Context.Headers.AllKeys.Contains("DiscordID"))
            {
                if (!Checks.IsValidID(Context.Headers["DiscordID"])) { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, DiscordID contains invalid characters"; return Context.ResponseObject; }
            }

            if (Context.URLSegments[1] == "viewer")//Check the url path for viewer
            {
                if (Context.Headers.AllKeys.Contains("ID")) // Get the viewer where header ID matches
                {
                    try { int.Parse(Context.Headers["ID"]); }//Check if the ID Header can be converted to an integer
                    catch {//If it cant be converted, set the contents of the Response Object to reflect this
                        Context.ResponseObject.Code = 400;
                        Context.ResponseObject.Message = "Bad Request, Malformed ID";
                        return Context.ResponseObject;
                    }
                    Data.Objects.Viewer B = Data.Objects.Viewer.FromID(int.Parse(Context.Headers["ID"]));//Fetch the Viewer Object with the given ID
                    if (B != null) { Context.ResponseObject.Data = B.ToJson(); }//If We get a Viewer back, set the Response Objects data to the JSON format of the Viewer
                    else {//If we didnt get a viewer back, set the contents of the Response Object to reflect that a viewer doesnt exist with the given ID
                        Context.ResponseObject.Code = 400;
                        Context.ResponseObject.Message = "Bad Request, ID does not match an existing object";
                        ErrorOccured = true;
                    }
                }
                else if ((Context.Headers.AllKeys.Contains("TwitchID") || Context.Headers.AllKeys.Contains("DiscordID")) && Context.Headers.AllKeys.Contains("CurrencyID")) // Get the viewer where header (TwitchID and/or DiscordID) and CurrencyID matches
                {
                    try { int.Parse(Context.Headers["CurrencyID"]); } catch { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Malformed CurrencyID"; return Context.ResponseObject; }
                    Data.Objects.Viewer B = Data.Objects.Viewer.FromTwitchDiscord(Context.Headers["DiscordID"], Context.Headers["TwitchID"], int.Parse(Context.Headers["CurrencyID"]));
                    if (B != null) { Context.ResponseObject.Data = B.ToJson(); }
                    else { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, TwitchID and/or DiscordID does not match an existing object"; ErrorOccured = true; }
                }
                else if (Context.Headers.AllKeys.Contains("CurrencyID")) // Get all viewers for the CurrencyID
                {
                    string OrderBy = null;
                    if (Context.Headers["Order"] == "WatchTime" || Context.Headers["Order"] == "Balance") { OrderBy = Context.Headers["Order"]; }
                    try { int.Parse(Context.Headers["CurrencyID"]); } catch { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Malformed CurrencyID"; return Context.ResponseObject; }
                    List<Data.Objects.Viewer> B = Data.Objects.Viewer.FromCurrency(int.Parse(Context.Headers["CurrencyID"]), OrderBy);
                    if (B.Count != 0) { Context.ResponseObject.Data = Newtonsoft.Json.Linq.JToken.FromObject(B); }
                    else { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, CurrencyID does not match an existing object"; ErrorOccured = true; }
                }
                else if (Context.Headers.AllKeys.Contains("TwitchID") || Context.Headers.AllKeys.Contains("DiscordID")) // Get all viewers for any currency where TwitchID and/or DiscordID matches
                {
                    List<Data.Objects.Viewer> B = Data.Objects.Viewer.FromTwitchDiscord(Context.Headers["DiscordID"], Context.Headers["TwitchID"]);
                    if (B.Count != 0) { Context.ResponseObject.Data = Newtonsoft.Json.Linq.JToken.FromObject(B); }
                    else { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, TwitchID and/or DiscordID does not match an existing object"; ErrorOccured = true; }
                }
                else//Inform requestor that we dont have any infomation to work with
                {
                    ErrorOccured = true;
                    Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, No operable Headers provided";
                }
            }
            else if (Context.URLSegments[1] == "currency")
            {
                if (Context.Headers.AllKeys.Contains("ID"))//Get Currency where ID matches
                {
                    try { int.Parse(Context.Headers["ID"]); } catch { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Malformed ID"; return Context.ResponseObject; }
                    Data.Objects.Currency C = Data.Objects.Currency.FromID(int.Parse(Context.Headers["ID"]));
                    if (Context.Headers.AllKeys.Contains("AccessToken") && Context.Headers.AllKeys.Contains("LoginID"))
                    { // If a valid accesstoken is provided, get private information
                        try { int.Parse(Context.Headers["LoginID"]); } catch { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Malformed ID"; return Context.ResponseObject; }
                        Data.Objects.Login L = Data.Objects.Login.FromID(int.Parse(Context.Headers["LoginID"]), true);
                        if (L != null)
                        {
                            if (Backend.Init.ScryptEncoder.Compare(Context.Headers["AccessToken"], L.AccessToken))
                            {
                                if (Data.Objects.Currency.FromLogin(L.ID).Find(x => x.ID == C.ID) != null) { C.LoadConfigs(true); }
                            }
                            else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, AccessToken is invalid"; }
                        }
                        else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, LoginID does not correspond to an existing user"; }
                    }
                    if (C != null) { Context.ResponseObject.Data = C.ToJson(); }
                    else { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, ID does not match an existing object"; ErrorOccured = true; }
                }
                else if (Context.Headers.AllKeys.Contains("LoginID"))// Get all Currencies of the LoginID
                {
                    try { int.Parse(Context.Headers["LoginID"]); } catch { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Malformed LoginID"; return Context.ResponseObject; }
                    List<Data.Objects.Currency> C = Data.Objects.Currency.FromLogin(int.Parse(Context.Headers["LoginID"]));
                    Context.ResponseObject.Data = Newtonsoft.Json.Linq.JToken.FromObject(C);
                    Context.ResponseObject.Code = 200; Context.ResponseObject.Message = "Unknown Outcome, It is not known if the LoginID matches an object"; ErrorOccured = true;
                }
                else//Inform requestor that we dont have any infomation to work with
                {
                    ErrorOccured = true;
                    Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, No operable Headers provided";
                }
            }
            else if (Context.URLSegments[1] == "login")
            {
                if (Context.Headers.AllKeys.Contains("ID"))//Get Login where ID matches
                {
                    try { int.Parse(Context.Headers["ID"]); } catch { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Malformed ID"; return Context.ResponseObject; }
                    Data.Objects.Login L = Data.Objects.Login.FromID(int.Parse(Context.Headers["ID"]));
                    if (L != null) { Context.ResponseObject.Data = L.ToJson(); }
                    if (Context.Headers.AllKeys.Contains("AccessToken")) {
                        if (Context.Headers["AccessToken"] != "")
                        {
                            L = Data.Objects.Login.FromID(int.Parse(Context.Headers["ID"]),true);
                            if (!Backend.Init.ScryptEncoder.Compare(Context.Headers["AccessToken"], L.AccessToken))
                            {
                                Context.ResponseObject = null;
                                Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, AccessToken doesnt match"; ErrorOccured = true;
                            }
                        }
                    }
                    else { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, ID does not match an existing object"; ErrorOccured = true; }
                }
                else if (Context.Headers.AllKeys.Contains("UserName"))//Get Login where UserName matches
                {
                    Data.Objects.Login L = Data.Objects.Login.FromUserName(Context.Headers["UserName"]);
                    if (L != null) { Context.ResponseObject.Data = L.ToJson(); }
                    else { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, UserName does not match an existing object"; ErrorOccured = true; }
                }
                else if (Context.Headers.AllKeys.Contains("Email"))//Get Login where Email matches
                {
                    Data.Objects.Login L = Data.Objects.Login.FromEmail(Context.Headers["Email"]);
                    if (L != null) { Context.ResponseObject.Data = L.ToJson(); }
                    else { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Email does not match an existing object"; ErrorOccured = true; }
                }
                else//Inform requestor that we dont have any infomation to work with
                {
                    ErrorOccured = true;
                    Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, No operable Headers provided";
                }
            }
            else if (Context.URLSegments[1] == "bot")
            {
                if (Context.Headers.AllKeys.Contains("ID") && Context.Headers.AllKeys.Contains("LoginID"))//Get Bot where ID matches
                {
                    bool WithSecretData = false;
                    try { int.Parse(Context.Headers["ID"]); } catch { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Malformed ID"; return Context.ResponseObject; }
                    if (Context.Headers.AllKeys.Contains("AccessToken"))// If a valid accesstoken is provided, get private information
                    {
                        try { int.Parse(Context.Headers["LoginID"]); } catch { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Malformed ID"; return Context.ResponseObject; }
                        Data.Objects.Login L = Data.Objects.Login.FromID(int.Parse(Context.Headers["LoginID"]), true);
                        if (L != null)
                        {
                            if (Backend.Init.ScryptEncoder.Compare(Context.Headers["AccessToken"], L.AccessToken))
                            {
                                if (Data.Objects.Bot.FromLogin(L.ID).Find(x => x.ID == int.Parse(Context.Headers["ID"])) != null) { WithSecretData = true; }
                            }
                            else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, AccessToken is invalid"; }
                        }
                        else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, LoginID does not correspond to an existing user"; }
                    }
                    Data.Objects.Bot B = Data.Objects.Bot.FromID(int.Parse(Context.Headers["ID"]), WithSecretData);
                    if (B != null) { Context.ResponseObject.Data = B.ToJson(); }
                    else { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, ID does not match an existing object"; ErrorOccured = true; }
                }
                else if (Context.Headers.AllKeys.Contains("LoginID"))//Get all Bots of LoginID
                {
                    try { int.Parse(Context.Headers["LoginID"]); } catch { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Malformed LoginID"; return Context.ResponseObject; }
                    List<Data.Objects.Bot> B = Data.Objects.Bot.FromLogin(int.Parse(Context.Headers["LoginID"]));
                    Context.ResponseObject.Data = Newtonsoft.Json.Linq.JToken.FromObject(B);
                    Context.ResponseObject.Code = 200; Context.ResponseObject.Message = "Unknown Outcome, It is not known if the LoginID matches an object"; ErrorOccured = true;
                }
                else if (Context.Headers.AllKeys.Contains("CurrencyID"))//Get all Bots of CurrencyID
                {
                    try { int.Parse(Context.Headers["CurrencyID"]); } catch { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Malformed CurrencyID"; return Context.ResponseObject; }
                    List<Data.Objects.Bot> B = Data.Objects.Bot.FromCurrency(int.Parse(Context.Headers["CurrencyID"]));
                    if (B.Count != 0) { Context.ResponseObject.Data = Newtonsoft.Json.Linq.JToken.FromObject(B); }
                    else { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, CurrencyID does not match an existing object"; ErrorOccured = true; }
                }
                else//Inform requestor that we dont have any infomation to work with
                {
                    ErrorOccured = true;
                    Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, No operable Headers provided";
                }
            }
            else if (Context.URLSegments[1] == "nightbot")
            {
                Context.GetStateParams();
                if (Context.URLParamaters.ContainsKey("code") && Context.URLParamaters.ContainsKey("state") && Context.StateParamaters.ContainsKey("currencyid") && Context.StateParamaters.ContainsKey("accesstoken"))
                {
                    string Code = Context.URLParamaters["code"];
                    try { int.Parse(Context.StateParamaters["currencyid"]); } catch { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Malformed CurrencyID"; return Context.ResponseObject; }
                    Data.Objects.Currency C = Data.Objects.Currency.FromID(int.Parse(Context.StateParamaters["currencyid"]));
                    if (C == null) { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, CurrencyID does not match an existing object"; ErrorOccured = true; }
                    else
                    {
                        Data.Objects.Login L = Data.Objects.Login.FromID(C.OwnerLogin.ID, true);
                        if (Backend.Init.ScryptEncoder.Compare(Context.StateParamaters["accesstoken"], L.AccessToken))
                        {
                            C.LoadConfigs(true);
                            WebRequest Req = WebRequest.Create("https://api.nightbot.tv/oauth2/token");
                            Req.Method = "POST";
                            byte[] PostData = Encoding.UTF8.GetBytes("client_id=" + C.LoginConfig["NightBot"]["ClientId"] +
                            "&client_secret=" + C.LoginConfig["NightBot"]["ClientSecret"] +
                            "&grant_type=authorization_code&redirect_uri=" + Backend.Init.APIConfig["WebURL"] + "/nightbot/&code=" + Code);
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
                        else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "AccessToken is not allowed to modify that currency"; }
                    }
                }
                else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Code and/or currencyid and/or accesstoken is missing"; }
            }
            else if (Context.URLSegments[1] == "streamlabs")
            {
                Context.GetStateParams();
                if (Context.URLParamaters.ContainsKey("code") && Context.URLParamaters.ContainsKey("state") && Context.StateParamaters.ContainsKey("currencyid"))
                {
                    string Code = Context.URLParamaters["code"];
                    try { int.Parse(Context.StateParamaters["currencyid"]); } catch { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Malformed CurrencyID"; return Context.ResponseObject; }
                    Data.Objects.Currency C = Data.Objects.Currency.FromID(int.Parse(Context.StateParamaters["currencyid"]));
                    if (C == null) { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, CurrencyID does not match an existing object"; ErrorOccured = true; }
                    else
                    {
                        Data.Objects.Login L = Data.Objects.Login.FromID(C.OwnerLogin.ID, true);
                        if (Backend.Init.ScryptEncoder.Compare(Context.StateParamaters["accesstoken"], L.AccessToken))
                        {
                            C.LoadConfigs(true);
                            WebRequest Req = WebRequest.Create("https://streamlabs.com/api/v1.0/token");
                            Req.Method = "POST";
                            Req.ContentType = "application/x-www-form-urlencoded";
                            byte[] PostData = Encoding.UTF8.GetBytes("grant_type=authorization_code&client_id=" + C.LoginConfig["StreamLabs"]["ClientId"] +
                                "&client_secret=" + C.LoginConfig["StreamLabs"]["ClientSecret"] +
                                "&redirect_uri=" + Backend.Init.APIConfig["WebURL"] + "/streamlabs/&code=" + Code);
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
                        else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "AccessToken is not allowed to modify that currency"; }
                    }
                }
                else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Code and/or currencyid and/or accesstoken is missing"; }
            }
            else if (Context.URLSegments[1] == "twitch")
            {
                Context.GetStateParams();
                if (Context.URLParamaters.ContainsKey("code") && Context.URLParamaters.ContainsKey("state") && Context.StateParamaters.ContainsKey("currencyid"))
                {
                    string Code = Context.URLParamaters["code"];
                    try { int.Parse(Context.StateParamaters["currencyid"]); } catch { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Malformed CurrencyID"; return Context.ResponseObject; }
                    Data.Objects.Currency C = Data.Objects.Currency.FromID(int.Parse(Context.StateParamaters["currencyid"]));
                    if (C == null) { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, CurrencyID does not match an existing object"; ErrorOccured = true; }
                    else
                    {
                        Data.Objects.Login L = Data.Objects.Login.FromID(C.OwnerLogin.ID, true);
                        if (Backend.Init.ScryptEncoder.Compare(Context.StateParamaters["accesstoken"], L.AccessToken))
                        {
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
                        else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "AccessToken is not allowed to modify that currency"; }
                    }
                }
                else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Code and/or currencyid and/or accesstoken is missing"; }
            }
            else//Inform requestor that the url does not got anywhere
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
