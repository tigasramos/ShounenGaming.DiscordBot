using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Server;
using Serilog;
using ShounenGaming.DiscordBot.Helpers;
using ShounenGaming.DiscordBot.Hubs;
using ShounenGaming.DiscordBot.Interactions;
using ShounenGaming.DiscordBot.Models;
using ShounenGaming.DiscordBot.Server.Models;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace ShounenGaming.DiscordBot.Handlers
{
    internal class DiscordEventsHandler
    {
        private static string CardPattern = @"<@(\d+)> \*\*grabbed\*\* the \*\*([^*]+)\*\* card";
        private static string VersionPattern = @"<@(\d+)> \*\*grabbed\*\* the \*\*([^*]+)\*\* version token";
        
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
        }

        /// <summary>
        /// Handle a Reaction on a BetterWishlist ping
        /// </summary>
        [Obsolete]
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
            await args.Message.DeleteReactionsEmojiAsync(args.Emoji);
        }

        /// <summary>
        /// Handle a Reaction on a SOFI ssl
        /// </summary>
        [Obsolete]
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
        [Obsolete]
        private async Task HandleTradeReaction(DiscordClient sender, MessageReactionAddEventArgs args)
        {
            if (args.Message.Author == null
                || args.Message.Author.Id != 853629533855809596
                || args.Message.ReferencedMessage == null
                || !args.Message.ReferencedMessage.Content.ToLowerInvariant().StartsWith("sg")
                || args.User.IsBot)
                return;

            if (args.Emoji.GetDiscordName() == ":x:")
            {
                var character = args.Message.Embeds[0].Description.Split("\n")[0].Split("**")[1];
                var show = args.Message.Embeds[0].Description.Split("\n")[1].Split("**")[1];

                await args.Message.RespondAsync($"```.wr {show} || {character}```");
                await args.Message.DeleteReactionsEmojiAsync(args.Emoji);
            }
            else return;
        }


        internal async Task HandleMessageReceived(DiscordClient sender, MessageCreateEventArgs args)
        {
            Log.Information($"{args.Author.Username} has sent a message");

            try
            {
                // From Nori
                if (args.Author.Id == 742070928111960155)
                {
                    // Drop Available
                    if (args.Message.Content.Contains("<@270355690680221706> You can now **drop**!"))
                    {
                        await SendMQTTMessage("drop-available");
                    }

                    // Someone Dropped a Card
                    if (args.Message.ReferencedMessage != null && args.Message.ReferencedMessage.Content.ToLower().Trim() == "sd")
                    {
                        foreach(var line in args.Message.Content.Split("\n"))
                        {
                            var tokens = line.Split('•');

                            // Show if Event (TODO), G < 100 or WL > 50
                            var wishlistNumberFound = int.TryParse(tokens[1].Replace(":heart:", "").Replace("`", ""), out int wishlistNumber);
                            var gNumberFound = int.TryParse(tokens[2].Replace("ɢ", "").Replace("`", ""), out int gNumber);

                            if ((wishlistNumberFound && wishlistNumber >= 100) ||
                                (gNumberFound && gNumber < 100))
                            {
                                var user = await args.Guild.GetMemberAsync(args.Message.ReferencedMessage.Author.Id);
                                await args.Guild.GetChannel(1259881894707724389).SendMessageAsync($"**{user.Username} ({user.Nickname})** dropped {tokens[3].Trim()} from **{tokens[4].Trim()}** with ({tokens[2].Trim()}) and **{wishlistNumber}** :heart:");
                                
                            }
                        }
                    }
                }

                // From SOFI
                if (args.Author.Id == 853629533855809596)
                {
                    // Drop Unavailable
                    if (args.Message.Content.Contains("<@270355690680221706> is **dropping** cards"))
                    {
                        await SendMQTTMessage("drop-unavailable");
                    }

                    // Someone Grabbed an Event Card 
                    if (args.Message.Content.Contains("(**Summer 2024") && args.Message.Content.Contains("grabbed"))
                    {
                        var user = await args.Guild.GetMemberAsync(args.Message.MentionedUsers[0].Id);
                        var cardName = args.Message.Content.Split("**grabbed** the")[1].Split("card (")[0].Trim();
                        await args.Guild.GetChannel(1259881894707724389).SendMessageAsync($"**{user.Username} ({user.Nickname})** grabbed {cardName} Card (Summer 2024 🏖️)");
                    }

                    // Someone Grabbed a Version
                    //match = Regex.Match(args.Message.Content, VersionPattern);
                    //if (match.Success)
                    //{
                    //    var user = await args.Guild.GetMemberAsync(Convert.ToUInt64(match.Groups[1].Value));
                    //    await args.Guild.GetChannel(1259881894707724389).SendMessageAsync($"**{user.Username} ({user.Nickname})** grabbed **{match.Groups[2].Value}** Version");
                    //}
                }

            }
            catch (Exception ex)
            {
                Log.Error($"Error Sending Message: {ex}");
            }
        }

        private async Task SendMQTTMessage(string topic, string payload = "")
        {
            var mqttOptions = services.GetRequiredService<MqttClientOptions>();
            var mqttClient = new MqttFactory().CreateMqttClient();
            await mqttClient.ConnectAsync(mqttOptions);

            var message = new MqttApplicationMessageBuilder()
               .WithTopic(topic)
               .WithPayload(payload)
               .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce)
               .WithRetainFlag(false)
               .Build();
            await mqttClient.PublishAsync(message, CancellationToken.None);
            await mqttClient.DisconnectAsync();
        }

        /// <summary>
        /// Handles dropped messages from BetterWishlist and adds emojis
        /// </summary>
        [Obsolete]
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
        [Obsolete]
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
        [Obsolete]
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
