﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace Twitch_Discord_Reward_Bot.Backend.Bots.DiscordBot
{
    public class Instance : BaseObject
    {
        public Instance(BotInstance BotInstance) : base(BotInstance)
        {

        }

        DiscordSocketClient Client;
        public async void StartBot()
        {
            DiscordSocketConfig SocketConfig = new DiscordSocketConfig();
            SocketConfig.AlwaysDownloadUsers = true;

            Client = new DiscordSocketClient(SocketConfig);
            await Client.LoginAsync(Discord.TokenType.Bot, BotInstance.LoginConfig["Discord"]["Bot"]["Token"].ToString());
            await Client.StartAsync();
        }
    }
}
