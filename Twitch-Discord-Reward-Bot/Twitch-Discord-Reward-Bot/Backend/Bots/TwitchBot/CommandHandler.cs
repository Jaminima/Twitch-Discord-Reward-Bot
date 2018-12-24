using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Client.Events;

namespace Twitch_Discord_Reward_Bot.Backend.Bots.TwitchBot
{
    public class CommandHandler:BaseObject
    {
        public CommandHandler(BotInstance BotInstance) : base(BotInstance) { }

        public void Handle(object sender, OnMessageReceivedArgs e)
        {
            new Thread(()=>HandleThread(e)).Start();
        }

        void HandleThread(OnMessageReceivedArgs e)
        {
            string[] SegmentedMessage = e.ChatMessage.Message.ToLower().Split(" ".ToCharArray());
            string Prefix = BotInstance.CommandConfig["Prefix"].ToString();

            if (SegmentedMessage[0].StartsWith(Prefix))
            {
                if (SegmentedMessage[0].EndsWith("echo"))
                {
                    SendMessage("@<SenderUser> @<OtherString>",e.ChatMessage,null,-1,-1,e.ChatMessage.Message.Replace(SegmentedMessage[0],""));
                }
            }
        }

        public void SendMessage(string ParamaterisedMessage, ChatMessage Message, string TargetUsername = null, int Amount = -1, int NewBal = -1, string OtherString = "", string SenderUsername=null)
        {
            ParamaterisedMessage = ParamaterisedMessage.Replace("@<OtherString>", OtherString);
            if (Message != null) { ParamaterisedMessage = ParamaterisedMessage.Replace("@<SenderUser>", "@" + Message.Username); }
            else { ParamaterisedMessage = ParamaterisedMessage.Replace("@<SenderUser>", "@" + SenderUsername); }
            ParamaterisedMessage = ParamaterisedMessage.Replace("@<CurrencyName>", BotInstance.CommandConfig["CurrencyName"].ToString());
            ParamaterisedMessage = ParamaterisedMessage.Replace("@<TargetUser>", "@" + TargetUsername);
            ParamaterisedMessage = ParamaterisedMessage.Replace("@<Amount>", Amount.ToString("N0"));
            ParamaterisedMessage = ParamaterisedMessage.Replace("@<NewBalance>", NewBal.ToString("N0"));
            ParamaterisedMessage = ParamaterisedMessage.Replace("@<Prefix>", BotInstance.CommandConfig["Prefix"].ToString());

            BotInstance.TwitchBot.Client.SendMessage(Message.Channel, ParamaterisedMessage);
        }
    }
}
