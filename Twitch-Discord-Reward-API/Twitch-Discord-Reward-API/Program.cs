using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Twitch_Discord_Reward_API
{
    class Program
    {
        static void Main(string[] args)
        {
            var D = new Backend.Data.Objects.Bot();
            D.OwnerLogin = Backend.Data.Objects.Login.FromID(1);
            D.Currency = Backend.Data.Objects.Currency.FromID(1);
            D.Save();
        }
    }
}
