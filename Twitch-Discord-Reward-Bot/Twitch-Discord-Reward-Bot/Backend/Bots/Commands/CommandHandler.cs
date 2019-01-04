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
                        if (BotInstance.CommandConfig["CommandSetup"]["Balance"]["Enabled"].ToString().ToLower() == "true" &&
                            BotInstance.CommandConfig["CommandSetup"]["Balance"]["Commands"].Where(x => x.ToString() == Command) != null)
                        {
                            if (e.SegmentedBody.Length == 1)
                            {
                                List<KeyValuePair<string, string>> Headers = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("CurrencyID",BotInstance.Currency.ID.ToString()) };
                                if (e.MessageType == MessageType.Twitch) { Headers.Add(new KeyValuePair<string, string>("TwitchID", e.SenderID)); }
                                if (e.MessageType == MessageType.Discord) { Headers.Add(new KeyValuePair<string, string>("DiscordID", e.SenderID)); }
                                Data.APIIntergrations.RewardCurrencyAPI.WebRequests.PostRequest("bank",Headers,true);
                                Data.APIIntergrations.RewardCurrencyAPI.ResponseObject RObj = Data.APIIntergrations.RewardCurrencyAPI.WebRequests.GetRequest("bank",Headers);
                                if (RObj.Code == 200)
                                {
                                    Data.APIIntergrations.RewardCurrencyAPI.Objects.Bank B = Data.APIIntergrations.RewardCurrencyAPI.Objects.Bank.FromJson(RObj.Data);
                                    await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Balance"]["Responses"]["OwnBalance"].ToString(),e,null,B.Balance);
                                }
                            }
                            else if (e.SegmentedBody.Length == 2)
                            {
                                StandardisedUser U = IDFromMessageSegment(e.SegmentedBody[1], e);
                                List<KeyValuePair<string, string>> Headers = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("CurrencyID", BotInstance.Currency.ID.ToString()) };
                                if (e.MessageType == MessageType.Twitch) { Headers.Add(new KeyValuePair<string, string>("TwitchID", U.ID)); }
                                if (e.MessageType == MessageType.Discord) { Headers.Add(new KeyValuePair<string, string>("DiscordID", U.ID)); }
                                Data.APIIntergrations.RewardCurrencyAPI.WebRequests.PostRequest("bank", Headers, true);
                                Data.APIIntergrations.RewardCurrencyAPI.ResponseObject RObj = Data.APIIntergrations.RewardCurrencyAPI.WebRequests.GetRequest("bank", Headers);
                                if (RObj.Code == 200)
                                {
                                    Data.APIIntergrations.RewardCurrencyAPI.Objects.Bank B = Data.APIIntergrations.RewardCurrencyAPI.Objects.Bank.FromJson(RObj.Data);
                                    await SendMessage(BotInstance.CommandConfig["CommandSetup"]["Balance"]["Responses"]["OtherBalance"].ToString(), e, U, B.Balance);
                                }
                            }
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
