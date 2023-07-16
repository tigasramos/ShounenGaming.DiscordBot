using DSharpPlus;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Interactivity;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using DSharpPlus.CommandsNext;
using Serilog;
using ShounenGaming.DiscordBot.Handlers;
using ShounenGaming.DiscordBot.Models;

namespace ShounenGaming.DiscordBot.Helpers
{
    internal static class DiscordBotHelper
    {
        public static DiscordClient CreateDiscordBot(AppSettings appSettings)
        {
            //Create and Configure bot
            var discord = new DiscordClient(new DiscordConfiguration()
            {
                Token = appSettings.Discord.Token,
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.All,
                LogUnknownEvents = false,
                LogTimestampFormat = "dd/MM/yyyy - HH:mm:ss"
            });

            //Interactivity
            discord.UseInteractivity(new InteractivityConfiguration()
            {
                PollBehaviour = PollBehaviour.KeepEmojis,
                Timeout = TimeSpan.FromSeconds(30)
            });

            return discord;
        }

        public static void RegisterEventsAndCommands(this DiscordClient discord, IServiceProvider services)
        {
            Log.Information("Configuring the Discord Connection");

            var appSettings = services.GetRequiredService<AppSettings>();
            var discordEventsHandler = services.GetRequiredService<DiscordEventsHandler>();

            //Commands
            var commands = discord.UseCommandsNext(new CommandsNextConfiguration()
            {
                Services = services,
                StringPrefixes = new[] { appSettings.Discord.Prefix },
            });

            commands.RegisterCommands(Assembly.GetExecutingAssembly());
            commands.CommandErrored += CmdErroredHandler;

            //Events
            discord.Ready += discordEventsHandler.HandleInitializing;
            discord.GuildMemberAdded += discordEventsHandler.HandleNewServerMember;
            discord.GuildMemberRemoved += discordEventsHandler.HandleRemovedServerMember;
            discord.GuildMemberUpdated += discordEventsHandler.HandleUpdatedServerMember;
            discord.MessageCreated += discordEventsHandler.HandleMessageReceived;
            discord.GuildAvailable += discordEventsHandler.HandleGuildAvailable;
            discord.PresenceUpdated += discordEventsHandler.HandleMemberStatusChanged;
            discord.VoiceStateUpdated += discordEventsHandler.HandleVoiceChat;

            discord.ComponentInteractionCreated += discordEventsHandler.HandleInteraction;
        }


        private static async Task CmdErroredHandler(CommandsNextExtension sender, CommandErrorEventArgs e)
        {
            Log.Fatal(e.Exception.Message);
        }

    }
}
