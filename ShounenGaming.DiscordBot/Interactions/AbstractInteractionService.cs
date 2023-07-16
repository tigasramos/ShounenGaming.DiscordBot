using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShounenGaming.DiscordBot.Models;

namespace ShounenGaming.DiscordBot.Interactions
{
    public abstract class AbstractInteractionService
    {
        protected readonly IMemoryCache cache;
        protected readonly AppSettings appSettings;


        protected AbstractInteractionService(AppSettings appSettings, IMemoryCache cache)
        {
            this.appSettings = appSettings;
            this.cache = cache;
        }

        protected AbstractInteractionService(AppSettings appSettings)
        {
            this.appSettings = appSettings;
        }

        public abstract Task HandleInteraction(DiscordClient sender, ComponentInteractionCreateEventArgs e);


        protected async Task UpdateEmbed(DiscordEmbed oldEmbed, DiscordInteraction interaction, bool accepted, string modMention)
        {
            var embedColor = accepted ? DiscordColor.Green : DiscordColor.Red;
            var embed = new DiscordEmbedBuilder(oldEmbed).WithColor(embedColor).Build();

            await interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                new DiscordInteractionResponseBuilder()
                .WithContent(string.Format("{0} by {1}", accepted ? "Accepted" : "Rejected", modMention))
                .AddEmbed(embed));

        }

    }
}
