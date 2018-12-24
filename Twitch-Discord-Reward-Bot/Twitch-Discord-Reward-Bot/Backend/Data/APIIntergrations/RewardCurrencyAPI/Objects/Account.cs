using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.OleDb;

namespace Twitch_Discord_Reward_Bot.Backend.Data.APIIntergrations.RewardCurrencyAPI.Objects
{
    public class Account : BaseObject
    {
        public uint AccountId;
        public uint Balance;
        public User User;
    }
}
