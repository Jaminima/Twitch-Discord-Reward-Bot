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
        public static Newtonsoft.Json.Linq.JToken APIConfig = Data.FileManager.ReadFile("./Data/Api.config.json");

        public static void Start()
        {
            Backend.Networking.HTTPServer.Init.Start();
            while (true)
            {
                Console.ReadLine();
            }
        }
    }
}
