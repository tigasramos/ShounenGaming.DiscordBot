using DSharpPlus;
using DSharpPlus.Entities;

namespace ShounenGaming.DiscordBot.Helpers
{
    internal class SofiBotHelper
    {
        public readonly static ulong SOFI_ID = 853629533855809596;
        private readonly static int DropCooldownMinutes = 8; 
        private readonly static int GrabCooldownMinutes = 4; 

        private readonly Dictionary<ulong, SofiTimersHolder> _usersTimers = new();

        private System.Timers.Timer _timer = new(1000);

        private readonly DiscordClient bot;
        public SofiBotHelper(DiscordClient bot)
        {
            this.bot = bot;
            //_timer.Elapsed += SecondPassed;
            //_timer.Start();
        }

        private async void SecondPassed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            var availableUsers = _usersTimers.Values.Where(v => v.Available != null && v.Available <= DateTime.UtcNow);
            foreach (var user in availableUsers)
            {
                user.Available = null;
                await SendPrivateMessageToUser(user.UserId, user.GuildId, user.ChannelId, "**Drop** and **Grab** are now available");
            }
        }
        private async Task SendPrivateMessageToUser(ulong userId, ulong guildId, ulong channelId, string message)
        {
            var guild = await bot.GetGuildAsync(guildId);
            var channel = guild.GetChannel(channelId);
            await channel.SendMessageAsync($"<@{userId}> {message}");
        }

        public void HandleSofiMessage(DiscordMessage message)
        {
            if (message.ReferencedMessage != null && message.ReferencedMessage.Content.ToLower() == "sd" && message.Embeds.Any())
            {
                UserDropped(message.ReferencedMessage.Author.Id, message.Channel.GuildId!.Value, message.ChannelId);
            }
            else if (message.Content.ToLower().Contains("grabbed") && message.MentionedUsers.Any())
            {
                UserGrabbed(message.MentionedUsers[0].Id, message.Channel.GuildId!.Value, message.ChannelId);
            }
        }

        private void UserGrabbed(ulong userId, ulong guildId, ulong channelId)
        {
            if (!_usersTimers.ContainsKey(userId))
                _usersTimers.Add(userId, new SofiTimersHolder { UserId = userId, ChannelId = channelId, GuildId = guildId });

            var timers = _usersTimers[userId];
            var finishTimer = DateTime.UtcNow.AddMinutes(GrabCooldownMinutes);
            if (timers.Available == null || timers.Available <= finishTimer)
            {
                timers.Available = finishTimer;
            }
        }

        private void UserDropped(ulong userId, ulong guildId, ulong channelId)
        {
            if (!_usersTimers.ContainsKey(userId))
                _usersTimers.Add(userId, new SofiTimersHolder { UserId = userId , ChannelId = channelId, GuildId = guildId });

            var timers = _usersTimers[userId];
            var finishTimer = DateTime.UtcNow.AddMinutes(DropCooldownMinutes);
            if (timers.Available == null || timers.Available <= finishTimer)
            {
                timers.Available = finishTimer;
            }
        }
    }

    internal class SofiTimersHolder
    {
        public DateTime? Available { get; set; }
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong UserId { get; set; }
    }
}
