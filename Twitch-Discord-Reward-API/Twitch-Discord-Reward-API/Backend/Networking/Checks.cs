using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Twitch_Discord_Reward_API.Backend.Networking
{
    public static class Checks
    {
        static Char[] NSet = "0123456789".ToCharArray();
        public static bool IsValidID(string ID)
        {
            foreach (Char C in ID)
            {
                if (!NSet.Contains(C)) { return false; }
            }
            return true;
        }
    }
}
