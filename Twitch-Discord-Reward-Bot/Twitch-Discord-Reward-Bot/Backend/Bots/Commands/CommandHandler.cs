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
            string Prefix = BotInstance.CommandConfig["Prefix"].ToString();

            if (e.SegmentedBody[0].StartsWith(Prefix))
            {
                if (e.SegmentedBody[0].EndsWith("echo"))
                {
                    await SendMessage("@<SenderUser> @<OtherString>", e, null, -1, -1, e.MessageBody.Replace(e.SegmentedBody[0], ""));
                }
            }
        }

        public async Task SendMessage(string ParamaterisedMessage, StandardisedMessageRequest e, string TargetUsername = null, int Amount = -1, int NewBal = -1, string OtherString = "", string SenderUsername = null)
        {
            ParamaterisedMessage = ParamaterisedMessage.Replace("@<OtherString>", OtherString);
            ParamaterisedMessage = ParamaterisedMessage.Replace("@<CurrencyName>", BotInstance.CommandConfig["CurrencyName"].ToString());
            ParamaterisedMessage = ParamaterisedMessage.Replace("@<TargetUser>", "@" + TargetUsername);
            ParamaterisedMessage = ParamaterisedMessage.Replace("@<Amount>", Amount.ToString("N0"));
            ParamaterisedMessage = ParamaterisedMessage.Replace("@<NewBalance>", NewBal.ToString("N0"));
            ParamaterisedMessage = ParamaterisedMessage.Replace("@<Prefix>", BotInstance.CommandConfig["Prefix"].ToString());


            if (e.MessageType == MessageType.Twitch)
            {
                if (e.SenderUserName != null) { ParamaterisedMessage = ParamaterisedMessage.Replace("@<SenderUser>", "@" + e.SenderUserName); }
                else { ParamaterisedMessage = ParamaterisedMessage.Replace("@<SenderUser>", "@" + SenderUsername); }
                BotInstance.TwitchBot.Client.SendMessage(e.ChannelName, ParamaterisedMessage);
            }
            else
            {
                ParamaterisedMessage = ParamaterisedMessage.Replace("/me", "");
                ParamaterisedMessage = ParamaterisedMessage.Replace("@<SenderUser>", "<@" + e.SenderID + ">");
                await e.DiscordRaw.Channel.SendMessageAsync(ParamaterisedMessage);
            }
        }
    }
}
