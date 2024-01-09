using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using ShounenGaming.DiscordBot.Helpers;
using ShounenGaming.DiscordBot.Hubs;
using ShounenGaming.DiscordBot.Interactions;
using ShounenGaming.DiscordBot.Models;
using ShounenGaming.DiscordBot.Server.Models;
using System.Data;

namespace ShounenGaming.DiscordBot.Handlers
{
    internal class DiscordEventsHandler
    {
        private readonly TimeSpan MAX_TIME_FULL_CHANNEL = TimeSpan.FromSeconds(30);

        private readonly IServiceProvider services;

        private readonly DiscordClient bot;
        private readonly DiscordSettings configuration;
        private readonly IMemoryCache cache;

        private readonly SofiBotHelper sofiBotHelper;
        private readonly DiscordEventsHub discordEventsHub;

        public DiscordEventsHandler(DiscordClient bot, AppSettings configuration, IMemoryCache cache, DiscordEventsHub discordEventsHub, SofiBotHelper sofiBotHelper, IServiceProvider services)
        {
            this.bot = bot;
            this.configuration = configuration.Discord;
            this.cache = cache;

            this.discordEventsHub = discordEventsHub;
            this.sofiBotHelper = sofiBotHelper;
            this.services = services;
        }
        #region Interaction
        internal async Task HandleInteraction(DiscordClient sender, ComponentInteractionCreateEventArgs e)
        {
            Log.Information($"{e.User.Username} interacted with {e.Id}");
            try
            {
                var keywords = e.Id.Split("_");
                var handler = keywords[0]! switch
                {
                    "registration" => (AbstractInteractionService)services.GetRequiredService(typeof(RegistrationInteraction)),
                    _ => throw new Exception("Interaction Handler Not Found"),
                };
                await handler.HandleInteraction(sender, e);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                await NotifyErrorToSender(e.Guild, e.User.Id);
            }
        }
        #endregion
       

        internal async Task HandleUpdatedServerMember(DiscordClient sender, GuildMemberUpdateEventArgs args)
        {
            Log.Information($"{args.Member.DisplayName} has just updated");

            var role = args.Member.IsOwner ? RolesEnum.ADMIN : (args.Member.Roles.Any(r => r.Name.Contains("Mods")) ? RolesEnum.MOD : RolesEnum.USER);
            await discordEventsHub.UpdateServerMember(args.Member.Id.ToString(), args.Member.AvatarUrl, args.Member.DisplayName, args.Member.Username, role);
        }

        internal async Task HandleMessageReceived(DiscordClient sender, MessageCreateEventArgs args)
        {
            Log.Information($"{args.Author.Username} has sent a message");

            //if (args.Author.Id == SofiBotHelper.SOFI_ID)
            //    sofiBotHelper.HandleSofiMessage(args.Message);
            
        }


        internal async Task HandleNewServerMember(DiscordClient sender, GuildMemberAddEventArgs args)
        {
            Log.Information($"{args.Member.DisplayName} has joined the Server {args.Guild.Name}");

            var role = args.Member.IsOwner ? RolesEnum.ADMIN : (args.Member.Roles.Any(r => r.Name.Contains("Mods")) ? RolesEnum.MOD : RolesEnum.USER);
            await discordEventsHub.UpdateServerMember(args.Member.Id.ToString(), args.Member.AvatarUrl, args.Member.DisplayName, args.Member.Username, role);
        }

        internal async Task HandleRemovedServerMember(DiscordClient sender, GuildMemberRemoveEventArgs args)
        {
            Log.Information($"{args.Member.DisplayName} has left the Server {args.Guild.Name}");

            await discordEventsHub.UpdateServerMember(args.Member.Id.ToString(), args.Member.AvatarUrl, args.Member.DisplayName, args.Member.Username, null);
        }

        internal async Task HandleInitializing(DiscordClient sender, ReadyEventArgs args)
        {
            await sender.UpdateStatusAsync(new DSharpPlus.Entities.DiscordActivity
            {
                ActivityType = DSharpPlus.Entities.ActivityType.Watching,
                Name = "One Piece",
            }, DSharpPlus.Entities.UserStatus.Online);
        }

        internal async Task HandleVoiceChat(DiscordClient sender, VoiceStateUpdateEventArgs args)
        {
            var hasJoined = args.After.Channel != null && args.Before.Channel == null;
            var hasLeft = args.Before.Channel != null && args.After.Channel == null;

            if (hasJoined)
                Log.Information($"{args.User.Username} has joined {args.Channel.Name}");
            if (hasLeft)
                Log.Information($"{args.User.Username} has left {args.Before.Channel.Name}");
            

            // TODO: Remove from call
        }

        internal async Task HandleMemberStatusChanged(DiscordClient sender, PresenceUpdateEventArgs args)
        {
            if (args.PresenceBefore.Status != args.PresenceAfter.Status) 
                Log.Information($"{args.User.Username} has just updated his Status from {args.PresenceBefore.Status} to {args.PresenceAfter.Status}");
        }

        internal async Task HandleGuildAvailable(DiscordClient sender, GuildCreateEventArgs args)
        {
            Log.Information($"Guild Available");

            while (!discordEventsHub.IsConnected)
            {
                await Task.Delay(1000);
            }

            foreach (var member in sender.Guilds.Values.SelectMany(g => g.Members.Values).Where(m => !m.IsBot))
            {
                var role = member.IsOwner ? RolesEnum.ADMIN : (member.Roles.Any(r => r.Name.Contains("Mods")) ? RolesEnum.MOD : RolesEnum.USER);
                await discordEventsHub.UpdateServerMember(member.Id.ToString(), member.AvatarUrl, member.DisplayName, member.Username, role);
            }
        }

        #region Private
        private static async Task NotifyErrorToSender(DiscordGuild guild, ulong userId)
        {
            var member = await guild.GetMemberAsync(userId);
            await member.SendMessageAsync("There was a problem handling your interaction. Contact the Admin.");
        }
        #endregion
    }
}
