using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Twitch_Discord_Reward_Bot.Backend.Bots
{
    public class BaseObject
    {
        protected BotInstance BotInstance;
        public BaseObject(BotInstance BotInstance)
        {
            this.BotInstance = BotInstance;
        }
    }
}
