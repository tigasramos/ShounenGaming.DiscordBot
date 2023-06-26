using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.AspNetCore.SignalR.Client;
using ShounenGaming.DiscordBot.Helpers;
using ShounenGaming.DiscordBot.Models;
using ShounenGaming.DiscordBot.Server.Models;

namespace ShounenGaming.DiscordBot.Hubs
{
    internal class DiscordEventsHub : BaseHub
    {
        private static readonly int VERIFY_TIME = 2;

        public DiscordEventsHub(AppSettings appSettings, LoginHelper loginHelper, DiscordClient bot) : base(appSettings, loginHelper, "discordEventsHub")
        {
            hub.On("SendVerifyAccount", async (string discordId, string fullName) =>
            {
                var member = bot.Guilds.Values.SelectMany(x => x.Members.Values).FirstOrDefault(m => m.Id.ToString() == discordId);
                if (member is null) return;

                var message = await member.SendMessageAsync($"A request to verify your account was created with the name **\"{fullName}\"**. If that's you please react to this message in the next {VERIFY_TIME} minutes.");
                var user = (message.Channel as DiscordDmChannel)?.Recipients[0];
                var interaction = await message.WaitForReactionAsync(user, TimeSpan.FromMinutes(VERIFY_TIME));
                if (!interaction.TimedOut)
                {
                    await hub.InvokeAsync("VerifyAccount", discordId);
                }
            });

            hub.On("SendToken", async (string discordId, string token, DateTime expireDate) =>
            {
                var member = bot.Guilds.Values.SelectMany(x => x.Members.Values).FirstOrDefault(m => m.Id.ToString() == discordId);
                if (member is null) return;

                await member.SendMessageAsync($"Login Token: **{token}** , expires at {expireDate:dd/MM/yyyy HH:mm}");
            });
        }


        public async Task UpdateServerMember(string discordId, string discordImageUrl, string displayName, string username, RolesEnum? role)
        {
            await hub.InvokeAsync("UpdateServerMember", discordId, discordImageUrl, displayName, username, role);
        }
    }

}
