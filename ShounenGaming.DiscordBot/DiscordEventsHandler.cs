using DSharpPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShounenGaming.DiscordBot
{
    internal class DiscordEventsHandler
    {
        public IServiceProvider Services;
        public DiscordEventsHandler(IServiceProvider services) 
        {
            Services = services;
        }
        public void SubscribeDiscordEvents(DiscordClient bot)
        {
            bot.GuildMemberAdded += HandleNewServerMember;
        }

        private Task HandleNewServerMember(DiscordClient sender, DSharpPlus.EventArgs.GuildMemberAddEventArgs args)
        {
            throw new NotImplementedException();
        }
    }
}
