using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Twitch_Discord_Reward_API.Backend.Networking
{
    public static class TokenSystem
    {
        static Char[] TokenChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();
        public static string CreateToken(int Length)
        {
            string S = "";
            for (int i = 0; i < Length; i++)
            {
                S += TokenChars[Init.Rnd.Next(0, TokenChars.Length)];
            }
            return S;
        }
    }
}
