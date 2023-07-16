using DSharpPlus.EventArgs;
using DSharpPlus;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShounenGaming.DiscordBot.Hubs;
using ShounenGaming.DiscordBot.Models;

namespace ShounenGaming.DiscordBot.Interactions
{
    internal class RegistrationInteraction : AbstractInteractionService
    {
        private readonly DiscordEventsHub _hub;

        public RegistrationInteraction(AppSettings appSettings, DiscordEventsHub hub) : base(appSettings)
        {
            _hub = hub;
        }

        public async override Task HandleInteraction(DiscordClient sender, ComponentInteractionCreateEventArgs e)
        {
            var keywords = e.Id.Split("_");
            var userId = keywords[1];

            //State
            bool? accepted = keywords[2] switch
            {
                "accept" => true,
                "reject" => false,
                _ => null
            };
            if (!accepted.HasValue)
                throw new Exception("Error with the Accept/Reject Buttons");

            //Info
            var embed = e.Message.Embeds.Single();
            var member = await e.Guild.GetMemberAsync(Convert.ToUInt64(userId));

            if (accepted.Value)
            {
                await _hub.AcceptAccountVerification(userId);
                await member.SendMessageAsync("Your **Registration Request** was approved.");
            }
            else
            {
                await _hub.RejectAccountVerification(userId);
                await member.SendMessageAsync("Your **Registration Request** was rejected.");
            }

            await UpdateEmbed(embed, e.Interaction, accepted.Value, e.User.Mention);
        }

    }
}
