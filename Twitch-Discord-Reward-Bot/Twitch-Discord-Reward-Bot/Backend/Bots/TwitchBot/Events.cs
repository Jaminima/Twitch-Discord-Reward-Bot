using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Client.Events;

namespace Twitch_Discord_Reward_Bot.Backend.Bots.TwitchBot
{
    public class Events : BaseObject
    {
        public Events(BotInstance BotInstance) : base(BotInstance)
        {
        }

        public async void SubGifted(object sender, OnGiftedSubscriptionArgs e)
        {
            StandardisedUser Gifter = new StandardisedUser(),
                Giftee = new StandardisedUser(); ;
            Gifter.ID = e.GiftedSubscription.Id; Gifter.UserName = e.GiftedSubscription.DisplayName;
            Giftee.ID = e.GiftedSubscription.MsgParamRecipientId; Giftee.UserName = e.GiftedSubscription.MsgParamRecipientDisplayName;
            int Reward = int.Parse(BotInstance.CommandConfig["AutoRewards"]["GiftSub"]["Reward"].ToString());
            Data.APIIntergrations.RewardCurrencyAPI.Objects.Viewer V = Data.APIIntergrations.RewardCurrencyAPI.Objects.Viewer.FromTwitchDiscord(MessageType.Twitch, BotInstance, Gifter.ID);
            if (V != null) { Data.APIIntergrations.RewardCurrencyAPI.Objects.Viewer.AdjustBalance(V, Reward, "+"); }
            await BotInstance.CommandHandler.SendMessage(BotInstance.CommandConfig["AutoRewards"]["GiftSub"]["Response"].ToString(), e.Channel.ToString(),MessageType.Twitch,Gifter,Reward,OtherString:"@"+Giftee);
            if (BotInstance.CommandConfig["AutoRewards"]["DiscordSubNotifications"].ToString() == "True")
            { await BotInstance.CommandHandler.SendMessage(BotInstance.CommandConfig["AutoRewards"]["GiftSub"]["Response"].ToString(), BotInstance.CommandConfig["Discord"]["NotificationChannel"].ToString(), MessageType.Discord, Gifter, Reward, OtherString: "<@" + Giftee.UserName +">"); }
        }

        public async void Subbed(object sender, OnNewSubscriberArgs e)
        {
            StandardisedUser Subber = new StandardisedUser();
            Subber.ID = e.Subscriber.UserId; Subber.UserName = e.Subscriber.DisplayName;
            int Reward = int.Parse(BotInstance.CommandConfig["AutoRewards"]["NewSub"]["Reward"].ToString());
            Data.APIIntergrations.RewardCurrencyAPI.Objects.Viewer V = Data.APIIntergrations.RewardCurrencyAPI.Objects.Viewer.FromTwitchDiscord(MessageType.Twitch, BotInstance, Subber.ID);
            if (V != null) { Data.APIIntergrations.RewardCurrencyAPI.Objects.Viewer.AdjustBalance(V, Reward, "+"); }
            await BotInstance.CommandHandler.SendMessage(BotInstance.CommandConfig["AutoRewards"]["NewSub"]["Response"].ToString(), e.Channel.ToString(), MessageType.Twitch, Subber, Reward);
            if (BotInstance.CommandConfig["AutoRewards"]["DiscordSubNotifications"].ToString() == "True")
            { await BotInstance.CommandHandler.SendMessage(BotInstance.CommandConfig["AutoRewards"]["NewSub"]["Response"].ToString(), BotInstance.CommandConfig["Discord"]["NotificationChannel"].ToString(), MessageType.Discord, Subber, Reward); }
        }
        public async void ReSubbed(object sender, OnReSubscriberArgs e)
        {
            StandardisedUser Subber = new StandardisedUser();
            Subber.ID = e.ReSubscriber.UserId; Subber.UserName = e.ReSubscriber.DisplayName;
            int Reward = int.Parse(BotInstance.CommandConfig["AutoRewards"]["ReSub"]["Reward"].ToString());
            Data.APIIntergrations.RewardCurrencyAPI.Objects.Viewer V = Data.APIIntergrations.RewardCurrencyAPI.Objects.Viewer.FromTwitchDiscord(MessageType.Twitch, BotInstance, Subber.ID);
            if (V != null) { Data.APIIntergrations.RewardCurrencyAPI.Objects.Viewer.AdjustBalance(V, Reward, "+"); }
            await BotInstance.CommandHandler.SendMessage(BotInstance.CommandConfig["AutoRewards"]["ReSub"]["Response"].ToString(), e.Channel.ToString(), MessageType.Twitch, Subber, Reward);
            if (BotInstance.CommandConfig["AutoRewards"]["DiscordSubNotifications"].ToString() == "True")
            { await BotInstance.CommandHandler.SendMessage(BotInstance.CommandConfig["AutoRewards"]["ReSub"]["Response"].ToString(), BotInstance.CommandConfig["Discord"]["NotificationChannel"].ToString(), MessageType.Discord, Subber, Reward); }
        }
    }
}
