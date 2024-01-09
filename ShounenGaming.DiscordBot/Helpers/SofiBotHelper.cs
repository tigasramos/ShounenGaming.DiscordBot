using DSharpPlus.Entities;

namespace ShounenGaming.DiscordBot.Helpers
{
    internal class SofiBotHelper
    {
        public readonly static ulong SOFI_ID = 853629533855809596;
        private readonly static int DropCooldownMinutes = 8; 
        private readonly static int GrabCooldownMinutes = 4; 

        private readonly Dictionary<ulong, SofiTimersHolder> _usersTimers = new();

        public void HandleSofiMessage(DiscordMessage message, Func<ulong, ulong, ulong, string, Task> responseFunction)
        {
            if (message.ReferencedMessage != null && message.ReferencedMessage.Content == "sd" && message.Embeds.Any())
            {
                UserDropped(message.ReferencedMessage.Author.Id, message.Channel.GuildId!.Value, message.ChannelId, responseFunction);
            }
            else if (message.Content.Contains("grabbed") && message.MentionedUsers.Any())
            {
                UserGrabbed(message.MentionedUsers[0].Id, message.Channel.GuildId!.Value, message.ChannelId, responseFunction);
            }
        }

        private void UserGrabbed(ulong userId, ulong guildId, ulong channelId, Func<ulong, ulong, ulong, string, Task> responseFunction)
        {
            if (!_usersTimers.ContainsKey(userId))
                _usersTimers.Add(userId, new SofiTimersHolder());

            var timers = _usersTimers[userId];
            if (timers.GrabTimer != null)
                return;

            timers.GrabTimer = new System.Timers.Timer(60 * 1000 * GrabCooldownMinutes);
            timers.GrabTimer.Elapsed += async (sender, e) =>
            {
                var timer = _usersTimers[userId];
                timer.GrabTimer?.Stop();
                timer.GrabTimer = null;

                if (timer.DropTimer == null)
                    await responseFunction.Invoke(userId, guildId, channelId, "**Drop** and **Grab** are now available");
            };
            timers.GrabTimer.Start();
        }

        private void UserDropped(ulong userId, ulong guildId, ulong channelId, Func<ulong, ulong, ulong, string, Task> responseFunction)
        {
            if (!_usersTimers.ContainsKey(userId))
                _usersTimers.Add(userId, new SofiTimersHolder());

            var timers = _usersTimers[userId];
            if (timers.DropTimer != null)
                return;

            timers.DropTimer = new System.Timers.Timer(60 * 1000 * DropCooldownMinutes);
            timers.DropTimer.Elapsed += async (sender, e) =>
            {
                var timer = _usersTimers[userId];
                timer.DropTimer?.Stop();
                timer.DropTimer = null;

                if (timer.GrabTimer == null)
                    await responseFunction.Invoke(userId, guildId, channelId, "**Drop** and **Grab** are now available");
            };
            timers.DropTimer.Start();
        }
    }

    internal class SofiTimersHolder
    {
        public System.Timers.Timer? DropTimer { get; set; }
        public System.Timers.Timer? GrabTimer { get; set; }
    }
}
