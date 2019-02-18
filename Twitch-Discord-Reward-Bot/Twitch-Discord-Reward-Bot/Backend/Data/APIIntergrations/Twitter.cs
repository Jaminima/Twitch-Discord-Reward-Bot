using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tweetinvi;

namespace Twitch_Discord_Reward_Bot.Backend.Data.APIIntergrations
{
    public static class Twitter
    {
        public static string GetLatestTweet(BotInstance BotInstance)
        {
            Auth.SetUserCredentials(
                BotInstance.LoginConfig["Twitter"]["ConsumerKey"].ToString(),
                BotInstance.LoginConfig["Twitter"]["ConsumerSecret"].ToString(),
                BotInstance.LoginConfig["Twitter"]["AccessToken"].ToString(),
                BotInstance.LoginConfig["Twitter"]["AccessSecret"].ToString()
            );
            Tweetinvi.Models.IUser TwitterUser = User.GetUserFromScreenName("TheHarbonator");
            Tweetinvi.Models.ITweet UsersLatestTweet = TwitterUser.GetUserTimeline(1).Last();
            return UsersLatestTweet.Url;
        }
    }
}
