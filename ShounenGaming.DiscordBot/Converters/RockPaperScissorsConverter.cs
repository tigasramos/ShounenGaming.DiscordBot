using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using ShounenGaming.DiscordBot.Models;

namespace ShounenGaming.DiscordBot.Converters
{
    internal class RockPaperScissorsConverter : IArgumentConverter<RockPaperScissorsEnum>
    {
        public Task<Optional<RockPaperScissorsEnum>> ConvertAsync(string value, CommandContext ctx)
        {
            switch (value.ToLower())
            {
                case "rock":
                case "r":
                    return Task.FromResult(Optional.FromValue(RockPaperScissorsEnum.ROCK));
                case "paper":
                case "p":
                    return Task.FromResult(Optional.FromValue(RockPaperScissorsEnum.PAPER));
                case "scissors":
                case "s":
                    return Task.FromResult(Optional.FromValue(RockPaperScissorsEnum.SCISSORS));
                default:
                    return Task.FromResult(Optional.FromNoValue<RockPaperScissorsEnum>());
            }
        }
    }
}