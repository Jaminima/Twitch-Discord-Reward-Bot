using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client.Events;
using Discord.WebSocket;

namespace Twitch_Discord_Reward_Bot.Backend.Bots
{
    public class StandardisedMessageRequest
    {

        public string MessageBody,ChannelID,ChannelName,SenderID,SenderUserName;
        public string[] SegmentedBody;
        public MessageType MessageType;
        public OnMessageReceivedArgs TwitchRaw;
        public SocketMessage DiscordRaw;

        public static StandardisedMessageRequest FromTwitch(OnMessageReceivedArgs e)
        {
            StandardisedMessageRequest S = new StandardisedMessageRequest();
            S.MessageBody = e.ChatMessage.Message;
            S.SegmentedBody = S.MessageBody.Split(" ".ToCharArray());
            S.MessageType = MessageType.Twitch;
            S.SenderID = e.ChatMessage.UserId;
            S.SenderUserName = e.ChatMessage.Username;
            S.TwitchRaw = e;
            S.ChannelName = e.ChatMessage.Channel;
            return S;
        }

        public static StandardisedMessageRequest FromDiscord(SocketMessage e)
        {
            StandardisedMessageRequest S = new StandardisedMessageRequest();
            S.MessageBody = e.Content;
            S.SegmentedBody = S.MessageBody.Split(" ".ToCharArray());
            S.MessageType = MessageType.Discord;
            S.SenderID = e.Author.Id.ToString();
            S.SenderUserName = e.Author.Username;
            S.DiscordRaw = e;
            S.ChannelID = e.Channel.Id.ToString();
            S.ChannelName = e.Channel.Name;
            return S;
        }
    }

    public enum MessageType
    {
        Discord,Twitch
    }
}
