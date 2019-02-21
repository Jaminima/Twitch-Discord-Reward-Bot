using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client.Events;
using Discord.WebSocket;
using System.Net;
using System.IO;

namespace Twitch_Discord_Reward_Bot.Backend.Bots
{
    public class StandardisedMessageRequest
    {
        public string MessageBody,ChannelID,ChannelName,SenderID,SenderUserName;
        public string[] SegmentedBody;
        public MessageType MessageType;
        public OnMessageReceivedArgs TwitchRaw;
        public SocketMessage DiscordRaw;
        public bool IsNewUser;
        public StandardisedUser User;
        public Data.APIIntergrations.RewardCurrencyAPI.Objects.Viewer Viewer;

        public static StandardisedMessageRequest FromTwitch(OnMessageReceivedArgs e,BotInstance BotInstance)
        {
            StandardisedMessageRequest S = new StandardisedMessageRequest();
            S.MessageBody = e.ChatMessage.Message;
            S.SegmentedBody = S.MessageBody.Split(" ".ToCharArray());
            S.MessageType = MessageType.Twitch;
            S.SenderID = e.ChatMessage.UserId;
            S.SenderUserName = e.ChatMessage.Username;
            S.TwitchRaw = e;
            S.ChannelName = e.ChatMessage.Channel;
            S.User = new StandardisedUser();
            S.User.ID = S.SenderID;
            S.User.UserName = S.SenderUserName;
            S.Viewer = Data.APIIntergrations.RewardCurrencyAPI.Objects.Viewer.FromTwitchDiscord(S,BotInstance,S.User.ID,ref S.IsNewUser);
            return S;
        }

        public static StandardisedMessageRequest FromDiscord(SocketMessage e, BotInstance BotInstance)
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
            S.User = new StandardisedUser();
            S.User.ID = S.SenderID;
            S.User.UserName = S.SenderUserName;
            S.Viewer = Data.APIIntergrations.RewardCurrencyAPI.Objects.Viewer.FromTwitchDiscord(S, BotInstance, S.User.ID, ref S.IsNewUser);
            return S;
        }
    }

    public class StandardisedUser
    {
        public string UserName;
        public string ID;

        public static StandardisedUser FromTwitchUsername(string MessageSegment, BotInstance BotInstance,int Depth=0)
        {
            if (Depth == 5) { return null; }
            string UserName = MessageSegment.Replace("@", "");
            try
            {
                WebRequest Req = WebRequest.Create("https://api.twitch.tv/helix/users?login=" + UserName);
                Req.Method = "GET"; Req.Headers.Add("Authorization", BotInstance.LoginConfig["Twitch"]["API"]["AuthToken"].ToString());
                WebResponse Res = Req.GetResponse();
                string StreamString = new StreamReader(Res.GetResponseStream()).ReadToEnd();
                Newtonsoft.Json.Linq.JToken JData = Newtonsoft.Json.Linq.JToken.Parse(StreamString);
                StandardisedUser U = new StandardisedUser();
                U.ID = JData["data"][0]["id"].ToString();
                U.UserName = UserName;
                return U;
            }
            catch { return null; FromTwitchUsername(MessageSegment, BotInstance, Depth+1); }
            return null;
        }

        public static StandardisedUser FromDiscordMention(string MessageSegment, BotInstance BotInstance)
        {
            StandardisedUser U = new StandardisedUser();
            U.ID = MessageSegment.Replace("<@", "").Replace(">", "").Replace("!","");
            return U;
        }
    }

    public enum MessageType
    {
        Discord,Twitch
    }
}
