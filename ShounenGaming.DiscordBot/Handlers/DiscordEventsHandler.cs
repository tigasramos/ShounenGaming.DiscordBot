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

        private readonly DiscordEventsHub discordEventsHub;

        public DiscordEventsHandler(DiscordClient bot, AppSettings configuration, IMemoryCache cache, DiscordEventsHub discordEventsHub, IServiceProvider services)
        {
            this.bot = bot;
            this.configuration = configuration.Discord;
            this.cache = cache;

            this.discordEventsHub = discordEventsHub;
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
        public async Task HandleModal(DiscordClient client, ModalSubmitEventArgs e)
        {
            await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

        }
        #endregion

        internal async Task HandleUpdatedServerMember(DiscordClient sender, GuildMemberUpdateEventArgs args)
        {
            Log.Information($"{args.Member.DisplayName} has just updated");

            var role = args.Member.IsOwner ? RolesEnum.ADMIN : (args.Member.Roles.Any(r => r.Name.Contains("Mods")) ? RolesEnum.MOD : RolesEnum.USER);
            await discordEventsHub.UpdateServerMember(args.Member.Id.ToString(), args.Member.AvatarUrl, args.Member.DisplayName, args.Member.Username, role);
        }

        internal async Task HandleReactionAdded(DiscordClient sender, MessageReactionAddEventArgs args)
        {
            Log.Information($"{args.User.Username} has reacted to a message");

            // Handle Messages
            await HandleWishlistReaction(sender, args);
            await HandleSeriesReaction(sender, args);
            await HandleTradeReaction(sender, args);
        }

        /// <summary>
        /// Handle a Reaction on a BetterWishlist ping
        /// </summary>
        private async Task HandleWishlistReaction(DiscordClient sender, MessageReactionAddEventArgs args)
        {
            if (args.Message.Author == null 
                || args.Message.Author.Id != 1219361361348530298
                || !args.Message.Content.Contains("dropping")
                || args.Message.ReferencedMessage == null
                || args.User.IsBot)
                return;

            int selectedIndex = -1;
            switch (args.Emoji.GetDiscordName())
            {
                case ":one:":
                    selectedIndex = 1;
                    break;

                case ":two:":
                    selectedIndex = 2;
                    break;

                case ":three:":
                    selectedIndex = 3;
                    break;

                default:
                    return;
            }

            var lines = args.Message.Content.Split("\n");
            var previousMessageLines = args.Message.ReferencedMessage.Content.Split("\n");
            if (lines.Length - 1 < selectedIndex) return;

            var lastDoublePointer = lines[selectedIndex].LastIndexOf(":");
            var character = lines[selectedIndex].Substring(0, lastDoublePointer);

            var previousLine = previousMessageLines.First(l => l.ToLowerInvariant().Contains(character.ToLowerInvariant()));
            var show = previousLine.Split("•").Last().Replace("\r\n", string.Empty).Trim();

            await args.Channel.SendMessageAsync($"```.wr {show} || {character}```");
            await args.Message.DeleteAllReactionsAsync(args.Emoji);
        }

        /// <summary>
        /// Handle a Reaction on a SOFI ssl
        /// </summary>
        private async Task HandleSeriesReaction(DiscordClient sender, MessageReactionAddEventArgs args)
        {
            if (args.Message.Author == null 
                || args.Message.Author.Id != 853629533855809596
                || args.Message.ReferencedMessage == null
                || !args.Message.ReferencedMessage.Content.ToLowerInvariant().StartsWith("ssl") 
                ||  args.User.Id != args.Message.ReferencedMessage.Author.Id)
                return;

            var action = "";
            var characters = args.Message.Embeds[0].Fields[0].Value.Split("\n");
            List<string> selectedCharacters = new();
            if (args.Emoji.GetDiscordName() == ":white_check_mark:")
            {
                // Add
                action = "a";
                selectedCharacters = characters.Where(l => !l.Contains("☑️")).Select(l => l.Split("•").Last().Replace("**", "").Trim()).ToList();
            }
            else if (args.Emoji.GetDiscordName() == ":x:")
            {
                // Remove
                action = "r";
                selectedCharacters = characters.Where(l => l.Contains("☑️")).Select(l => l.Split("•").Last().Replace("**", "").Trim()).ToList();
            }
            else return;

            var nextMessages = await args.Message.Channel.GetMessagesAfterAsync(args.Message.Id);
            var responsesToEmbedMessage = nextMessages.Where(m => m.ReferencedMessage != null && m.ReferencedMessage.Id == args.Message.Id).ToList();
            var responseMessageForAction = responsesToEmbedMessage.LastOrDefault(r => r.Content.Contains($".w{action}"));

            if (responseMessageForAction != null)
            {
                var splittedMessage = responseMessageForAction.Content.Replace("```", "").Split("||");
                var allCharacters = splittedMessage[1].Split(",").ToList();
                allCharacters.AddRange(selectedCharacters);
                allCharacters = allCharacters.Select(c => c.Trim()).Distinct().ToList();
                await responseMessageForAction.ModifyAsync($"```{splittedMessage[0].Trim()} || {allCharacters.Aggregate((a, b) => a + ", " + b)}```");
            } 
            else
            { 
                // Add new Message
                var show = args.Message.Embeds[0].Description.Split("\n")[0].Split(": **").Last().Replace("**", "");
                await args.Message.RespondAsync($"```.w{action} {show} || {selectedCharacters.Aggregate((a, b) => a + ", " + b)}```");
            }

            await args.Message.DeleteReactionAsync(args.Emoji, args.User);
        }

        /// <summary>
        /// Handle a Reaction on a SOFI sg
        /// </summary>
        private async Task HandleTradeReaction(DiscordClient sender, MessageReactionAddEventArgs args)
        {
            if (args.Message.Author == null
                || args.Message.Author.Id != 853629533855809596
                || args.Message.ReferencedMessage == null
                || !args.Message.ReferencedMessage.Content.ToLowerInvariant().StartsWith("sg"))
                return;

            if (args.Emoji.GetDiscordName() == ":x:")
            {
                var character = args.Message.Embeds[0].Description.Split("\n")[0].Split("**")[1];
                var show = args.Message.Embeds[0].Description.Split("\n")[1].Split("**")[1];

                await args.Message.RespondAsync($"```.wr {show} || {character}```");
                await args.Message.DeleteAllReactionsAsync(args.Emoji);
            }
            else return;
        }


        internal async Task HandleMessageReceived(DiscordClient sender, MessageCreateEventArgs args)
        {
            Log.Information($"{args.Author.Username} has sent a message");

            // Handle Messages
            await HandleWishlistMessage(sender, args);
            await HandleTradeMessage(sender, args);
            await HandleSeriesMessage(sender, args);
        }

        /// <summary>
        /// Handles dropped messages from BetterWishlist and adds emojis
        /// </summary>
        private async Task HandleWishlistMessage(DiscordClient sender, MessageCreateEventArgs args)
        {
            if (args.Author.Id != 1219361361348530298
                || !args.Message.Content.Contains("dropping")
                || args.Message.ReferencedMessage == null)
                return;

            int lines = args.Message.Content.Split("\n").Length - 1;
            if (lines == 0) return;

            await args.Message.CreateReactionAsync(DiscordEmoji.FromName(sender, ":one:"));

            if (lines > 1)
                await args.Message.CreateReactionAsync(DiscordEmoji.FromName(sender, ":two:"));

            if (lines > 2)
                await args.Message.CreateReactionAsync(DiscordEmoji.FromName(sender, ":three:"));

        }

        /// <summary>
        /// Handles sg from SOFI and adds emojis
        /// </summary>
        private async Task HandleTradeMessage(DiscordClient sender, MessageCreateEventArgs args)
        {
            if (args.Author.Id != 853629533855809596
                || args.Message.ReferencedMessage == null
                || !args.Message.ReferencedMessage.Content.ToLowerInvariant().StartsWith("sg"))
                return;

            // Add Emoji to Fetch for the remaining
            await args.Message.CreateReactionAsync(DiscordEmoji.FromName(sender, ":x:"));

        }

        /// <summary>
        /// Handles ssl from SOFI and adds emojis
        /// </summary>
        private async Task HandleSeriesMessage(DiscordClient sender, MessageCreateEventArgs args)
        {
            if (args.Author.Id != 853629533855809596
                || args.Message.ReferencedMessage == null 
                || !args.Message.ReferencedMessage.Content.ToLowerInvariant().StartsWith("ssl"))
                return;

            // Add Emoji to Fetch for the remaining
            await args.Message.CreateReactionAsync(DiscordEmoji.FromName(sender, ":white_check_mark:"));
            await args.Message.CreateReactionAsync(DiscordEmoji.FromName(sender, ":x:"));

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
            var hasJoined = (args.After != null && args.After.Channel != null) && (args.Before == null || args.Before.Channel == null);
            var hasLeft = (args.Before != null && args.Before.Channel != null) && (args.After == null || args.After.Channel == null);

            if (hasJoined)
                Log.Information($"{args.User.Username} has joined {args.Channel.Name}");
            if (hasLeft)
                Log.Information($"{args.User.Username} has left {args.Before.Channel.Name}");
            

            // TODO: Remove from call
        }

        internal async Task HandleMemberStatusChanged(DiscordClient sender, PresenceUpdateEventArgs args)
        {
            if (args.PresenceBefore != null && args.PresenceAfter != null && args.PresenceBefore.Status != args.PresenceAfter.Status) 
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
