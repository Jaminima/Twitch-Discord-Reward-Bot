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
        public static Data.SQL SQLi = new Data.SQL("./Data/Database"); // Create an instance of the sql object, that will be used everywhere
        public static Newtonsoft.Json.Linq.JToken APIConfig = Data.FileManager.ReadFile("./Data/Api.config.json"); // Read the API's master config from storage
        public static Scrypt.ScryptEncoder ScryptEncoder = new Scrypt.ScryptEncoder(); // Create an instance of the ScryptEncoder

        public static void Start()
        {
            Networking.HTTPServer.Init.Start(); // Start the HTTPServer
            while (true)
            {
                Console.ReadLine();
            }
        }
    }
}
