using System;

namespace Twitch_Discord_Reward_Bot
{
    class Program
    {
        static void Main(string[] args)
        {
            Backend.Init.Start();
            while (true)
            {
                Console.ReadLine();
            }
        }
    }
}
