using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Twitch_Discord_Reward_Bot.Backend.Data.APIIntergrations
{
    public class AccessToken
    {
        public string Token;
        public DateTime ExpiresAt;

        public AccessToken(string Token,int ExpiresIn)
        {
            ExpiresAt = DateTime.Now;
            ExpiresAt = ExpiresAt.AddSeconds((double)ExpiresIn);
            this.Token = Token;
        }
    }
}
