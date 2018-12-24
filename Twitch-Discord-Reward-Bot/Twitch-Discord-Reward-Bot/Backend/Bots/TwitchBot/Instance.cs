using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client;
using TwitchLib.Client.Models;

namespace Twitch_Discord_Reward_Bot.Backend.Bots.TwitchBot
{
    public class Instance:BaseObject
    {
        CommandHandler HandlerInstance;
        public Instance(BotInstance BotInstance):base(BotInstance)
        {
            HandlerInstance = new CommandHandler(BotInstance);
            StartBot();
        }


        public TwitchClient Client;
        void StartBot()
        {
            ConnectionCredentials BotDetails = new ConnectionCredentials(
                BotInstance.LoginConfig["Twitch"]["Bot"]["Username"].ToString(),
                BotInstance.LoginConfig["Twitch"]["Bot"]["AuthToken"].ToString()
                );
            Client = new TwitchClient();
            Client.Initialize(BotDetails,BotInstance.CommandConfig["ChannelName"].ToString());
            Client.OnMessageReceived += HandlerInstance.Handle;
            Client.Connect();
        }
    }
}
