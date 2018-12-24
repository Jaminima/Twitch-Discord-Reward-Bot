using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.OleDb;

namespace Twitch_Discord_Reward_Bot.Backend.Data.APIIntergrations.RewardCurrencyAPI.Objects
{
    public class User : BaseObject
    {
        public uint UserId;
        public string TwitchId, DiscordId;
        public Account Account;
    }
}
