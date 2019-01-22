using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Twitch_Discord_Reward_API.Backend.Networking.HTTPServer
{
    public  static class Post
    {
        public static ResponseObject Handle(StandardisedRequestObject Context)
        {
            bool ErrorOccured = false;
            Backend.Data.Objects.Bot CorrespondingBot = AuthCheck(Context);

            if (Context.Headers.AllKeys.Contains("TwitchID"))
            {
                if (!Checks.IsValidID(Context.Headers["TwitchID"])) { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, TwitchID contains invalid characters"; return Context.ResponseObject; }
            }
            if (Context.Headers.AllKeys.Contains("DiscordID"))
            {
                if (!Checks.IsValidID(Context.Headers["DiscordID"])) { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, DiscordID contains invalid characters"; return Context.ResponseObject; }
            }

            if (Context.URLSegments[1] == "viewer")
            {
                if ((Context.Headers.AllKeys.Contains("TwitchID") || Context.Headers.AllKeys.Contains("DiscordID") || Context.Headers.AllKeys.Contains("Notifications") || Context.Headers.AllKeys.Contains("WatchTime")) && Context.Headers.AllKeys.Contains("ID"))
                {
                    if (CorrespondingBot != null)
                    {
                        Data.Objects.Viewer B = Data.Objects.Viewer.FromID(int.Parse(Context.Headers["ID"]));
                        if (B.Currency.ID == CorrespondingBot.Currency.ID || CorrespondingBot.IsSuperBot)
                        {
                            if (Context.Headers["DiscordID"] != null) { B.DiscordID = Context.Headers["DiscordID"]; }
                            if (Context.Headers["TwitchID"] != null) { B.TwitchID = Context.Headers["TwitchID"]; }
                            if (Context.Headers["Notifications"] != null) { B.LiveNotifcations = Context.Headers["Notifications"] == "True"; }
                            if (Context.Headers["WatchTime"] != null) { B.WatchTime = int.Parse(Context.Headers["WatchTime"]); }
                            B.Update();
                        }
                        else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, This bot does not have permission to edit that Bank"; }
                    }
                    else
                    {
                        ErrorOccured = true;
                        Context.ResponseObject.Code = 403; Context.ResponseObject.Message = "Invalid AuthToken";
                    }
                }
                else if (Context.Headers.AllKeys.Contains("TwitchID") || Context.Headers.AllKeys.Contains("DiscordID"))
                {
                    if (CorrespondingBot != null)
                    {
                        Data.Objects.Viewer B = new Data.Objects.Viewer();
                        B.DiscordID = Context.Headers["DiscordID"];
                        B.TwitchID = Context.Headers["TwitchID"];
                        if (Context.Headers.AllKeys.Contains("CurrencyID"))
                        {
                            try { int.Parse(Context.Headers["CurrencyID"]); } catch { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Malformed CurrencyID"; return Context.ResponseObject; }
                            if (int.Parse(Context.Headers["CurrencyID"]) == CorrespondingBot.Currency.ID || CorrespondingBot.IsSuperBot)
                            {
                                B.Currency = Data.Objects.Currency.FromID(int.Parse(Context.Headers["CurrencyID"]));
                            }
                            else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, This bot does not have permission to edit that Currency"; }
                        }
                        else { B.Currency = CorrespondingBot.Currency; }
                        B.Balance = int.Parse(CorrespondingBot.Currency.CommandConfig["InititalBalance"].ToString());
                        if (B.Currency != null) { B.Save(); }
                        else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, was unable to set Currency, try explicitly setting Currency with CurrencyID header"; }
                    }
                    else
                    {
                        ErrorOccured = true;
                        Context.ResponseObject.Code = 403; Context.ResponseObject.Message = "Invalid AuthToken";
                    }
                }
                else if (Context.Headers.AllKeys.Contains("ID") && Context.Headers.AllKeys.Contains("Operator") && Context.Headers.AllKeys.Contains("Value"))
                {
                    if (CorrespondingBot != null)
                    {
                        try { int.Parse(Context.Headers["ID"]); int.Parse(Context.Headers["Value"]); } catch { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Malformed ID and/or Value"; return Context.ResponseObject; }
                        Data.Objects.Viewer B = Data.Objects.Viewer.FromID(int.Parse(Context.Headers["ID"]));
                        if (B.Currency.ID == CorrespondingBot.Currency.ID || CorrespondingBot.IsSuperBot)
                        {
                            if (Context.Headers["Operator"].ToString() == "+")
                            {
                                B.Balance += int.Parse(Context.Headers["Value"]);
                                if (B.Balance >= 0) { B.Update(); }
                                else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Cannot set balance as negative"; }
                            }
                            else if (Context.Headers["Operator"].ToString() == "-")
                            {
                                B.Balance -= int.Parse(Context.Headers["Value"]);
                                if (B.Balance >= 0) { B.Update(); }
                                else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Cannot set balance as negative"; }
                            }
                            else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Operator must be + or -"; }
                        }
                        else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, This bot does not have permission to edit that Bank"; }
                    }
                    else
                    {
                        ErrorOccured = true;
                        Context.ResponseObject.Code = 403; Context.ResponseObject.Message = "Invalid AuthToken";
                    }
                }
                else if (Context.Headers.AllKeys.Contains("ID"))
                {
                    if (CorrespondingBot != null)
                    {
                        try { int.Parse(Context.Headers["ID"]); } catch { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Malformed ID"; return Context.ResponseObject; }
                        Data.Objects.Viewer B = Data.Objects.Viewer.FromID(int.Parse(Context.Headers["ID"]));
                        if (B.Currency.ID == CorrespondingBot.Currency.ID || CorrespondingBot.IsSuperBot)
                        {
                            B.Delete();
                        }
                        else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, This bot does not have permission to edit that Bank"; }
                    }
                    else
                    {
                        ErrorOccured = true;
                        Context.ResponseObject.Code = 403; Context.ResponseObject.Message = "Invalid AuthToken";
                    }
                }
                else
                {
                    ErrorOccured = true;
                    Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, No operable Headers provided";
                }
            }
            else if (Context.URLSegments[1] == "login")
            {
                if ((Context.Headers.AllKeys.Contains("UserName") || Context.Headers.AllKeys.Contains("Email") || Context.Headers.AllKeys.Contains("Password")) && Context.Headers.AllKeys.Contains("AccessToken"))
                {
                    Data.Objects.Login L = Data.Objects.Login.FromAccessToken(Context.Headers["AccessToken"]);
                    if (L != null)
                    {
                        if (Context.Headers["Email"] != null) { L.Email = Context.Headers["Email"]; }
                        if (Context.Headers["UserName"] != null) { L.UserName = Context.Headers["UserName"]; }
                        if (Context.Headers["HashedPassword"] != null) { L.HashedPassword = new Scrypt.ScryptEncoder().Encode(Context.Headers["Password"]); }
                        if (!L.UpdateUserNameEmailPassword()) { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, That UserName or Email may be in use by another account"; }
                    }
                    else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, AccessToken is invalid"; }
                }
                else if (Context.Headers.AllKeys.Contains("Password"))
                {
                    if (Context.Headers.AllKeys.Contains("UserName"))
                    {
                        Data.Objects.Login L = Data.Objects.Login.FromUserName(Context.Headers["UserName"], true);
                        if (L != null)
                        {
                            if (Backend.Init.ScryptEncoder.Compare(Context.Headers["Password"],L.HashedPassword)) { L.UpdateToken(); L.HashedPassword = null; Context.ResponseObject.Data = L.ToJson(); }
                            else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Password does not match"; }
                        }
                        else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, UserName does not correspond to an existing user"; }
                    }
                    else if (Context.Headers.AllKeys.Contains("Email"))
                    {
                        Data.Objects.Login L = Data.Objects.Login.FromEmail(Context.Headers["Email"], true);
                        if (L != null)
                        {
                            if (Backend.Init.ScryptEncoder.Compare(Context.Headers["Password"], L.HashedPassword)) { L.UpdateToken(); L.HashedPassword = null; Context.ResponseObject.Data = L.ToJson(); }
                            else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Password does not match"; }
                        }
                        else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Email does not correspond to an existing user"; }
                    }
                    else
                    {
                        ErrorOccured = true;
                        Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Email or UserName header is required";
                    }
                }
                else if (Context.URLSegments.Length == 3)
                {
                    if (Context.Headers.AllKeys.Contains("AccessToken") && Context.URLSegments[2] == "delete")
                    {
                        Data.Objects.Login L = Data.Objects.Login.FromAccessToken(Context.Headers["AccessToken"]);
                        if (L != null)
                        {
                            L.Delete();
                        }
                        else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, AccessToken is invalid"; }
                    }
                }
                else
                {
                    ErrorOccured = true;
                    Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, No operable Headers provided";
                }
            }
            else if (Context.URLSegments[1] == "signup")
            {
                if ((Context.Headers.AllKeys.Contains("UserName") || Context.Headers.AllKeys.Contains("Email")) && Context.Headers.AllKeys.Contains("Password"))
                {
                    Backend.Data.Objects.Login L = new Data.Objects.Login();
                    L.Email = Context.Headers["Email"];
                    L.UserName = Context.Headers["UserName"];
                    if (Data.Objects.Login.FromEmail(L.Email) == null && Data.Objects.Login.FromUserName(L.UserName) == null)
                    {
                        L.HashedPassword = Backend.Init.ScryptEncoder.Encode(Context.Headers["Password"]);
                        L.Save();
                    }
                    else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, User already exists"; }
                }
                else
                {
                    ErrorOccured = true;
                    Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, No operable Headers provided";
                }
            }
            else if (Context.URLSegments[1] == "bot")
            {
                if (Context.Headers.AllKeys.Contains("RefreshToken"))
                {
                    Backend.Data.Objects.Bot B = Backend.Data.Objects.Bot.FromRefreshToken(Context.Headers["RefreshToken"]);
                    if (B != null)
                    {
                        B.PerformRefresh();
                        Context.ResponseObject.Data = B.ToJson();
                    }
                    else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Refresh Token does not correspond to a bot"; }
                }
                else if (Context.Headers.AllKeys.Contains("AccessToken") && Context.Headers.AllKeys.Contains("InviteCode") && Context.Headers.AllKeys.Contains("CurrencyID"))
                {
                    try { int.Parse(Context.Headers["CurrencyID"]); } catch { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Malformed CurrencyID"; return Context.ResponseObject; }
                    Data.Objects.Login L = Data.Objects.Login.FromAccessToken(Context.Headers["AccessToken"]);
                    if (L != null)
                    {
                        Data.Objects.Bot B = Data.Objects.Bot.FromInviteCode(Context.Headers["InviteCode"]);
                        if (B != null)
                        {
                            if (B.Currency == null)
                            {
                                B.Currency = Data.Objects.Currency.FromLogin(L.ID).Find(x => x.ID == int.Parse(Context.Headers["CurrencyID"]));
                                if (B.Currency == null) { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, AccessToken is not allowed to edit that currency"; }
                                else { B.UpdateCurrency(); }
                            }
                            else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Bot is already bound to a currency"; }
                        }
                        else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, InviteCode doesnt match any bot"; }
                    }
                    else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, AccessToken is invalid"; }
                }
                else if (Context.Headers.AllKeys.Contains("AccessToken"))
                {
                    Data.Objects.Login L = Data.Objects.Login.FromAccessToken(Context.Headers["AccessToken"]);
                    if (L != null)
                    {
                        Data.Objects.Bot B = new Data.Objects.Bot();
                        B.OwnerLogin = Data.Objects.Login.FromID(L.ID);
                        B.Save();
                        B = Data.Objects.Bot.FromLogin(L.ID, true).Last();
                        Context.ResponseObject.Data = B.ToJson();
                    }
                    else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, AccessToken is invalid"; }
                }
                else
                {
                    ErrorOccured = true;
                    Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, No operable Headers provided";
                }
            }
            else if (Context.URLSegments[1] == "currency")
            {
                if (Context.URLSegments.Length == 3)
                {
                    if (Context.URLSegments[2] == "all")
                    {
                        if (CorrespondingBot!=null&&CorrespondingBot.IsSuperBot) { Context.ResponseObject.Data = Newtonsoft.Json.Linq.JToken.FromObject(Data.Objects.Currency.All(true)); }
                        else { Context.ResponseObject.Data = Newtonsoft.Json.Linq.JToken.FromObject(Data.Objects.Currency.All()); }
                    }
                    else
                    {
                        ErrorOccured = true;
                        Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Bot is not SuperBot";
                    }
                }
                else if ((Context.Headers.AllKeys.Contains("AccessToken")||CorrespondingBot!=null) && Context.RequestData != null && Context.Headers.AllKeys.Contains("CurrencyID"))
                {
                    try { int.Parse(Context.Headers["CurrencyID"]); } catch { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Malformed CurrencyID"; return Context.ResponseObject; }
                    Data.Objects.Login L = null;
                    if (Context.Headers.AllKeys.Contains("AccessToken")) { L = Data.Objects.Login.FromAccessToken(Context.Headers["AccessToken"]); }
                    if (L != null||CorrespondingBot!=null)
                    {
                        Data.Objects.Currency B = Data.Objects.Currency.FromID(int.Parse(Context.Headers["CurrencyID"]));
                        B.LoadConfigs(true);
                        bool LoginGood = false, BotGood = false; ;
                        if (L != null) { LoginGood = B.OwnerLogin.ID == L.ID; }
                        if (CorrespondingBot != null) { BotGood = CorrespondingBot.Currency.ID == B.ID || CorrespondingBot.IsSuperBot; }
                        if (LoginGood||BotGood)
                        {
                            if (Context.RequestData["LoginConfig"] != null)
                            {
                                if (Checks.JSONLayoutCompare(
                                    Newtonsoft.Json.Linq.JToken.Parse(System.IO.File.ReadAllText("./Data/DefaultConfigs/Login.config.json")),
                                    Context.RequestData["LoginConfig"])) { B.LoginConfig = Context.RequestData["LoginConfig"]; }
                                else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, LoginConfig does not follow the required structure"; }
                            }
                            if (Context.RequestData["CommandConfig"] != null)
                            {
                                if (Checks.JSONLayoutCompare(
                                    Newtonsoft.Json.Linq.JToken.Parse(System.IO.File.ReadAllText("./Data/DefaultConfigs/Command.config.json")),
                                    Context.RequestData["CommandConfig"])) { B.CommandConfig = Context.RequestData["CommandConfig"]; }
                                else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, ComamndConfig does not follow the required structure"; }
                            }
                            if (ErrorOccured == false) { B.UpdateConfigs(); }
                        }
                        else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, This login does not have permission to edit that Currency"; }
                    }
                    else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, AccessToken is invalid"; }
                }
                else if (Context.Headers.AllKeys.Contains("CurrencyID") && CorrespondingBot != null)
                {
                    try { int.Parse(Context.Headers["CurrencyID"]); } catch { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Malformed CurrencyID"; return Context.ResponseObject; }
                    Data.Objects.Currency C = Data.Objects.Currency.FromID(int.Parse(Context.Headers["CurrencyID"]));
                    if (CorrespondingBot.Currency.ID == C.ID || CorrespondingBot.IsSuperBot)
                    {
                        C.LoadConfigs(true);
                        Context.ResponseObject.Data = C.ToJson();
                    }
                    else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, This bot does not have permission to read that Currency"; }
                }
                else if (Context.Headers.AllKeys.Contains("AccessToken"))
                {
                    Data.Objects.Login L = Data.Objects.Login.FromAccessToken(Context.Headers["AccessToken"]);
                    if (L != null)
                    {
                        Data.Objects.Currency B = new Data.Objects.Currency();
                        B.OwnerLogin = Data.Objects.Login.FromID(L.ID);
                        B.Save();
                        B = Data.Objects.Currency.FromLogin(L.ID).Last();
                        Context.ResponseObject.Data = B.ToJson();
                    }
                    else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, AccessToken is invalid"; }
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

        static Data.Objects.Bot AuthCheck(StandardisedRequestObject Context)
        {
            if (Context.Headers.AllKeys.Contains("AuthToken"))
            {
                return Data.Objects.Bot.FromAccessToken(Context.Headers["AuthToken"]);
            }
            else { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, AuthToken is missing"; return null; }
        }
    }
}
