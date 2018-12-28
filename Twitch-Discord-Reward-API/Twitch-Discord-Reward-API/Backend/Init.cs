using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Twitch_Discord_Reward_API.Backend
{
    public static class Init
    {
        public static Random Rnd = new Random();
        public static Data.SQL SQLi = new Data.SQL("./Data/Database");
    }
}
