﻿using System;
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

            if (Context.URLSegments[1] == "bank")
            {
                if ((Context.Headers.AllKeys.Contains("TwitchID") || Context.Headers.AllKeys.Contains("DiscordID")) && Context.Headers.AllKeys.Contains("ID"))
                {
                    if (CorrespondingBot != null)
                    {
                        Data.Objects.Bank B = Data.Objects.Bank.FromID(int.Parse(Context.Headers["ID"]));
                        if (B.Currency.ID == CorrespondingBot.Currency.ID || CorrespondingBot.IsSuperBot)
                        {
                            if (Context.Headers["DiscordID"] != null) { B.DiscordID = Context.Headers["DiscordID"]; }
                            if (Context.Headers["TwitchID"] != null) { B.TwitchID = Context.Headers["TwitchID"]; }
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
                        Data.Objects.Bank B = new Data.Objects.Bank();
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
                        Data.Objects.Bank B = Data.Objects.Bank.FromID(int.Parse(Context.Headers["ID"]));
                        if (B.Currency.ID == CorrespondingBot.Currency.ID || CorrespondingBot.IsSuperBot)
                        {
                            if (Context.Headers["Operator"].ToString() == "+") { B.Balance += int.Parse(Context.Headers["Value"]); B.Update(); }
                            else if (Context.Headers["Operator"].ToString() == "-") { B.Balance -= int.Parse(Context.Headers["Value"]); B.Update(); }
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
                else
                {
                    ErrorOccured = true;
                    Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, No operable Headers provided";
                }
            }
            else if (Context.URLSegments[1] == "login")
            {
                if ((Context.Headers.AllKeys.Contains("UserName") || Context.Headers.AllKeys.Contains("Email") || Context.Headers.AllKeys.Contains("HashedPassword")) && Context.Headers.AllKeys.Contains("AccessToken"))
                {
                    Data.Objects.Login L = Data.Objects.Login.FromAccessToken(Context.Headers["AccessToken"]);
                    if (L != null)
                    {
                        if (Context.Headers["Email"] != null) { L.Email = Context.Headers["Email"]; }
                        if (Context.Headers["UserName"] != null) { L.UserName = Context.Headers["UserName"]; }
                        if (Context.Headers["HashedPassword"] != null) { L.HashedPassword = Context.Headers["HashedPassword"]; }
                        if (!L.UpdateUserNameEmailPassword()) { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, That UserName or Email may be in use by another account"; }
                    }
                    else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, AccessToken is invalid"; }
                }
                else if (Context.Headers.AllKeys.Contains("HashedPassword"))
                {
                    if (Context.Headers.AllKeys.Contains("UserName"))
                    {
                        Data.Objects.Login L = Data.Objects.Login.FromUserName(Context.Headers["UserName"], true);
                        if (L != null)
                        {
                            if (L.HashedPassword == Context.Headers["HashedPassword"]) { L.UpdateToken(); L.HashedPassword = null; Context.ResponseObject.Data = L.ToJson(); }
                            else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, HashedPassword does not match"; }
                        }
                        else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, UserName does not correspond to an existing user"; }
                    }
                    else if (Context.Headers.AllKeys.Contains("Email"))
                    {
                        Data.Objects.Login L = Data.Objects.Login.FromEmail(Context.Headers["Email"], true);
                        if (L != null)
                        {
                            if (L.HashedPassword == Context.Headers["HashedPassword"]) { L.UpdateToken(); L.HashedPassword = null; Context.ResponseObject.Data = L.ToJson(); }
                            else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, HashedPassword does not match"; }
                        }
                        else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Email does not correspond to an existing user"; }
                    }
                    else
                    {
                        ErrorOccured = true;
                        Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Email or UserName header is required";
                    }
                }
                else if (Context.URLSegments.Length == 3) { 
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
                if ((Context.Headers.AllKeys.Contains("UserName") || Context.Headers.AllKeys.Contains("Email")) && Context.Headers.AllKeys.Contains("HashedPassword"))
                {
                    Backend.Data.Objects.Login L = new Data.Objects.Login();
                    L.Email = Context.Headers["Email"];
                    L.UserName = Context.Headers["UserName"];
                    L.HashedPassword = Context.Headers["HashedPassword"];
                    L.Save();
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
                            B = Data.Objects.Bot.FromLogin(L.ID,true).Last();
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
                if (Context.URLSegments.Length == 3 && CorrespondingBot != null)
                {
                    if (Context.URLSegments[2] == "all" && CorrespondingBot.IsSuperBot)
                    {
                        Context.ResponseObject.Data = Newtonsoft.Json.Linq.JToken.FromObject(Data.Objects.Currency.All(true));
                    }
                    else
                    {
                        ErrorOccured = true;
                        Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Bot is not SuperBot";
                    }
                }
                else if (Context.Headers.AllKeys.Contains("AccessToken") && Context.RequestData != null && Context.Headers.AllKeys.Contains("CurrencyID"))
                {
                    try { int.Parse(Context.Headers["CurrencyID"]); } catch { Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, Malformed CurrencyID"; return Context.ResponseObject; }
                    Data.Objects.Login L = Data.Objects.Login.FromAccessToken(Context.Headers["AccessToken"]);
                    if (L != null)
                    {
                        Data.Objects.Currency B = Data.Objects.Currency.FromID(int.Parse(Context.Headers["CurrencyID"]));
                        B.LoadConfigs(true);
                        if (B.OwnerLogin.ID == L.ID)
                        {
                            if (Context.RequestData["LoginConfig"] != null)
                            {
                                B.LoginConfig = Context.RequestData["LoginConfig"];
                            }
                            if (Context.RequestData["CommandConfig"] != null)
                            {
                                B.CommandConfig = Context.RequestData["CommandConfig"];
                            }
                            B.UpdateConfigs();
                        }
                        else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, This login does not have permission to edit that Currency"; }
                    }
                    else { ErrorOccured = true; Context.ResponseObject.Code = 400; Context.ResponseObject.Message = "Bad Request, AccessToken is invalid"; }
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
