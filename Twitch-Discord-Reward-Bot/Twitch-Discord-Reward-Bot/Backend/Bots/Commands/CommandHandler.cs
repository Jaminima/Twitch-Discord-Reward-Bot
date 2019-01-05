using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Client.Events;
using Discord.WebSocket;

namespace Twitch_Discord_Reward_Bot.Backend.Bots.Commands
{
    using Objects = Data.APIIntergrations.RewardCurrencyAPI.Objects;
    public class CommandHandler : BaseObject
    {
        public CommandHandler(BotInstance BotInstance) : base(BotInstance) { }


        public void Handle(object sender, OnMessageReceivedArgs e)
        {
            Handle(sender, StandardisedMessageRequest.FromTwitch(e));
        }
        public async Task Handle(SocketMessage e)
        {
            Handle(null, StandardisedMessageRequest.FromDiscord(e));
        }

        public void Handle(object sender, StandardisedMessageRequest e)
        {
            new Thread(async () => await HandleThread(e)).Start();
        }

        async Task HandleThread(StandardisedMessageRequest e)
        {
            try
            {
                if (e.SenderID != BotInstance.DiscordBot.Client.CurrentUser.Id.ToString())
                {
                    string Prefix = BotInstance.CommandConfig["Prefix"].ToString();
                    string Command = e.SegmentedBody[0].Replace(Prefix, "").ToLower();
                    
                    if (e.MessageType==MessageType.Discord && BotInstance.CommandConfig["DiscordChannels"].Where(x => x.ToString() == e.ChannelID).Count()==0) { return; }

                    if (e.SegmentedBody[0].StartsWith(Prefix))
                    {
                        if (CommandEnabled(BotInstance.CommandConfig["CommandSetup"]["Balance"],e) &&
                            JArrayContainsString(BotInstance.CommandConfig["CommandSetup"]["Balance"]["Commands"],Command))
                        {
                            if (e.SegmentedBody.Length == 1)
                            {
                                Objects.Bank B = Objects.Bank.FromTwitchDiscord(e, BotInstance, e.SenderID);
                                if (B!=null)
                                {
                                    await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Balance"]["Responses"]["OwnBalance"].ToString(),e,null,B.Balance);
                                }
                                else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["APIError"].ToString(), e); }
                            }
                            else if (e.SegmentedBody.Length == 2)
                            {
                                StandardisedUser U = IDFromMessageSegment(e.SegmentedBody[1], e);
                                if (U.ID != null)
                                {
                                    Objects.Bank B = Objects.Bank.FromTwitchDiscord(e, BotInstance, U.ID);
                                    if (B!=null)
                                    {
                                        await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Balance"]["Responses"]["OtherBalance"].ToString(), e, U, B.Balance);
                                    }
                                    else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["APIError"].ToString(), e); }
                                }
                                else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["CannotFindUser"].ToString(), e); }
                            }
                            else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["ParamaterCount"].ToString(), e); }
                        }
                        if (CommandEnabled(BotInstance.CommandConfig["CommandSetup"]["Pay"], e) &&
                            JArrayContainsString(BotInstance.CommandConfig["CommandSetup"]["Pay"]["Commands"], Command))
                        {
                            if (e.SegmentedBody.Length == 3)
                            {
                                StandardisedUser U = IDFromMessageSegment(e.SegmentedBody[1], e);
                                if (U.ID != null)
                                {
                                    Objects.Bank Self = Objects.Bank.FromTwitchDiscord(e, BotInstance, e.SenderID),
                                        Other = Objects.Bank.FromTwitchDiscord(e,BotInstance,U.ID);
                                    try { int.Parse(e.SegmentedBody[2]); } catch { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["NumberParamaterInvalid"].ToString(), e); return; }
                                    int ChangeBy = int.Parse(e.SegmentedBody[2]), MinPayment= int.Parse(BotInstance.CommandConfig["CommandSetup"]["Pay"]["MinimumPayment"].ToString());
                                    if (ChangeBy >= MinPayment)
                                    {
                                        if (ChangeBy >= 0)
                                        {
                                            if (Self.Balance - ChangeBy >= 0)
                                            {
                                                if (Objects.Bank.AdjustBalance(Self, ChangeBy, "-"))
                                                {
                                                    if (Objects.Bank.AdjustBalance(Other, ChangeBy, "+"))
                                                    {
                                                        await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Pay"]["Responses"]["Paid"].ToString(), e, U, ChangeBy);
                                                    }
                                                    else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["APIError"].ToString(), e); }
                                                }
                                                else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["APIError"].ToString(), e); }
                                            }
                                            else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Pay"]["Responses"]["NotEnough"].ToString(), e); }
                                        }
                                        else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["NumberParamaterNegative"].ToString(), e); }
                                    }
                                    else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Pay"]["Responses"]["TooSmall"].ToString(), e,null,MinPayment); }
                                }
                                else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["CannotFindUser"].ToString(), e); }
                            }
                            else { await SendMessage(BotInstance.CommandConfig["CommandSetup"]["ErrorResponses"]["ParamaterCount"].ToString(), e); }
                        }
                    }
                }
            }
            catch (Exception E) { Console.WriteLine(E); }
        }

        public StandardisedUser IDFromMessageSegment(string MessageSegment, StandardisedMessageRequest e)
        {
            if (e.MessageType == MessageType.Discord)
            {
                return StandardisedUser.FromDiscordMention(MessageSegment, BotInstance);
            }
            else if (e.MessageType == MessageType.Twitch)
            {
                return StandardisedUser.FromTwitchUsername(MessageSegment, BotInstance);
            }
            return null;
        }

        public bool JArrayContainsString(Newtonsoft.Json.Linq.JToken Array,string S)
        {
            foreach (Newtonsoft.Json.Linq.JToken Item in Array)
            {
                if (Item.ToString() == S) { return true; }
            }
            return false;
        }

        public bool CommandEnabled(Newtonsoft.Json.Linq.JToken Command,StandardisedMessageRequest e)
        {
            if (e.MessageType == MessageType.Discord)
            {
                if (Command["DiscordEnabled"].ToString().ToLower() == "true") { return true; }
            }
            if (e.MessageType == MessageType.Twitch)
            {
                if (Command["TwitchEnabled"].ToString().ToLower() == "true") { return true; }
            }
            return false;
        }

        public async Task SendMessage(string ParamaterisedMessage, StandardisedMessageRequest e, StandardisedUser TargetUser = null, int Amount = -1, int NewBal = -1, string OtherString = "", string SenderUsername = null)
        {
            ParamaterisedMessage = ParamaterisedMessage.Replace("@<OtherString>", OtherString);
            ParamaterisedMessage = ParamaterisedMessage.Replace("@<CurrencyName>", BotInstance.CommandConfig["CurrencyName"].ToString());
            ParamaterisedMessage = ParamaterisedMessage.Replace("@<Amount>", Amount.ToString("N0"));
            ParamaterisedMessage = ParamaterisedMessage.Replace("@<NewBalance>", NewBal.ToString("N0"));
            ParamaterisedMessage = ParamaterisedMessage.Replace("@<CurrencyAcronym>", BotInstance.CommandConfig["CurrencyAcronym"].ToString());


            if (e.MessageType == MessageType.Twitch)
            {
                if (TargetUser != null) { ParamaterisedMessage = ParamaterisedMessage.Replace("@<TargetUser>", "@" + TargetUser.UserName); }
                if (e.SenderUserName != null) { ParamaterisedMessage = ParamaterisedMessage.Replace("@<SenderUser>", "@" + e.SenderUserName); }
                else { ParamaterisedMessage = ParamaterisedMessage.Replace("@<SenderUser>", "@" + SenderUsername); }
                BotInstance.TwitchBot.Client.SendMessage(e.ChannelName, ParamaterisedMessage);
            }
            else
            {
                if (TargetUser != null) { ParamaterisedMessage = ParamaterisedMessage.Replace("@<TargetUser>", "<@" + TargetUser.ID + ">"); }
                ParamaterisedMessage = ParamaterisedMessage.Replace("/me", "");
                ParamaterisedMessage = ParamaterisedMessage.Replace("@<SenderUser>", "<@" + e.SenderID + ">");
                await e.DiscordRaw.Channel.SendMessageAsync(ParamaterisedMessage);
            }
        }
    }
}
