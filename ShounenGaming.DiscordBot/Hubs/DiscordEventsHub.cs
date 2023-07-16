using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.AspNetCore.SignalR.Client;
using Serilog;
using ShounenGaming.DiscordBot.Helpers;
using ShounenGaming.DiscordBot.Models;
using ShounenGaming.DiscordBot.Server.Models;
using System.Data;

namespace ShounenGaming.DiscordBot.Hubs
{
    internal class DiscordEventsHub : BaseHub
    {

        public DiscordEventsHub(AppSettings appSettings, LoginHelper loginHelper, DiscordClient bot) : base(appSettings, loginHelper, "discordEventsHub")
        {
            hub.On("SendVerifyAccount", async (string discordId, string fullName, DateTime birthday) =>
            {
                Log.Information("SendVerifyAccount received");
                try
                {
                    var guild = bot.Guilds.Values.Where(x => x.Members.Values.Any(m => m.Id.ToString() == discordId)).FirstOrDefault();
                    var member = guild?.Members.Values.FirstOrDefault(m => m.Id.ToString() == discordId);
                    if (member is null)
                    {
                        Log.Error($"{discordId} was not found on any server");
                        return;
                    }

                    // Send Message in Mods Channel
                    var modsChannelId = appSettings.Discord.ServerSettings.ModsChannelId;
                    var modsChannel = guild!.GetChannel(Convert.ToUInt64(modsChannelId));
                    var embedMessage = new DiscordEmbedBuilder()
                            .WithTitle("User Registration")
                            .WithDescription($"**User:** {member.Mention}\n**Full Name:** {fullName}\n**Birthday:** {birthday:dd/MM/yyyy}")
                            .WithThumbnail(member.AvatarUrl)
                            .WithFooter(DateTime.Now.ToString()).Build();

                    var modsRole = guild.Roles.Values.FirstOrDefault(r => r.Id.ToString() == appSettings.Discord.ServerSettings.ModsRoleId);
                    if (modsRole is null)
                    {
                        Log.Error($"Mods Role was not found on the server");
                        return;
                    };

                    var builder = new DiscordMessageBuilder()
                            .WithEmbed(embedMessage)
                            .WithContent($"<@&{modsRole.Id}>")
                            .AddComponents(new DiscordComponent[]
                            {
                            new DiscordButtonComponent(ButtonStyle.Success, $"registration_{member.Id}_accept", "Accept"),
                            new DiscordButtonComponent(ButtonStyle.Danger, $"registration_{member.Id}_reject", "Reject"),
                            });

                    var newMessage = await modsChannel.SendMessageAsync(builder);

                    // Expire previous one
                    var allBeforeMessages = await modsChannel.GetMessagesBeforeAsync(newMessage.Id);
                    foreach (var message in allBeforeMessages)
                    {
                        if (!message.Author.IsBot) continue;
                        if (message.Embeds.Any())
                        {
                            foreach (var row in message.Components)
                            {
                                if (row.Components.Any(c => c.CustomId.Contains(member.Id.ToString())))
                                {
                                    await message.DeleteAsync();
                                }
                            }
                        }
                    }

                    await member.SendMessageAsync("Your request was sent and is being validated by a Mod");
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            });

            hub.On("SendToken", async (string discordId, string token, DateTime expireDate) =>
            {
                Log.Information("SendToken received");
                var member = bot.Guilds.Values.SelectMany(x => x.Members.Values).FirstOrDefault(m => m.Id.ToString() == discordId);
                if (member is null) return;

                await member.SendMessageAsync($"Login Token: **{token}** expires at *{expireDate:dd/MM/yyyy HH:mm}*");
            });
        }

        public async Task AcceptAccountVerification(string discordId)
        {
            await hub.InvokeAsync("AcceptAccountVerification", discordId);

        }
        public async Task RejectAccountVerification(string discordId)
        {
            await hub.InvokeAsync("RejectAccountVerification", discordId);

        }

        public async Task UpdateServerMember(string discordId, string discordImageUrl, string displayName, string username, RolesEnum? role)
        {
            await hub.InvokeAsync("UpdateServerMember", discordId, discordImageUrl, displayName, username, role);
        }
    }

}
