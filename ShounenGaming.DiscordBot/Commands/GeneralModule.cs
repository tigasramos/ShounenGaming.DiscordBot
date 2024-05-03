using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext;
using ShounenGaming.DiscordBot.Server.Services;
using Serilog;
using DSharpPlus.Entities;
using ShounenGaming.DiscordBot.Models;

namespace ShounenGaming.DiscordBot.Commands
{
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class GeneralModule : BaseCommandModule
    {
        public IAuthService authService { private get; set; }

        [Command("register")]
        public async Task RegisterCommand(CommandContext ctx, string firstName, string lastName, string birthday)
        {
            //Parse birthday
            List<string>? birthdaySeparated = birthday.Split('-').ToList();

            if (birthdaySeparated.Count != 3) birthdaySeparated = birthday.Split('/').ToList();
            if (birthdaySeparated.Count != 3)
            {
                await ctx.RespondAsync("Wrong Date Formatting.");
                return;
            }

            try
            {
                await authService.RegisterUser(new Server.Models.CreateUser
                {
                    Birthday = new DateTime(Convert.ToInt32(birthdaySeparated[2]), Convert.ToInt32(birthdaySeparated[1]), Convert.ToInt32(birthdaySeparated[0]), 0, 0, 0, DateTimeKind.Utc),
                    FirstName = firstName.Trim(),
                    LastName = lastName.Trim(),
                    DiscordId = ctx.Message.Author.Id.ToString()
                });

                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":thumbsup:"));
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                await ctx.RespondAsync("There was an error processing your request.");
            }
          
        }
        
        [Command("rps")]
        public async Task RockPaperScissorsGame(CommandContext ctx, DiscordUser user)
        {
            // TODO: Embed Message to play Rock Paper Scissors with another User
        }

        [Command("rps")]
        public async Task RockPaperScissorsGame(CommandContext ctx, RockPaperScissorsEnum play)
        {
            var enumValues = Enum.GetValues(typeof(RockPaperScissorsEnum));
            var randomGenerator = new Random().Next(enumValues.Length);
            var cpuValue = (RockPaperScissorsEnum)randomGenerator;

            var playEmoji = ConvertRockPaperScissorsToEmoji(play);
            var cpuEmoji = ConvertRockPaperScissorsToEmoji(cpuValue);

            var gameState = "Lost";
            if ((play == RockPaperScissorsEnum.ROCK && cpuValue == RockPaperScissorsEnum.SCISSORS)
                || (play == RockPaperScissorsEnum.PAPER && cpuValue == RockPaperScissorsEnum.ROCK)
                || (play == RockPaperScissorsEnum.SCISSORS && cpuValue == RockPaperScissorsEnum.SCISSORS))
                gameState = "Won";
            else if ((play == RockPaperScissorsEnum.ROCK && cpuValue == RockPaperScissorsEnum.ROCK)
                || (play == RockPaperScissorsEnum.PAPER && cpuValue == RockPaperScissorsEnum.PAPER)
                || (play == RockPaperScissorsEnum.SCISSORS && cpuValue == RockPaperScissorsEnum.SCISSORS))
                gameState = "Draw";

            await ctx.RespondAsync($"You: {playEmoji} vs Bot: {cpuEmoji}\nYou {gameState}!");
        }
        private string ConvertRockPaperScissorsToEmoji(RockPaperScissorsEnum play)
        {
            return play switch
            {
                RockPaperScissorsEnum.ROCK => ":rock:",
                RockPaperScissorsEnum.PAPER => ":newspaper:",
                RockPaperScissorsEnum.SCISSORS => ":scissors:",
                _ => ":question:",
            };
        }

        [Command("random")]
        public async Task RandomNumber(CommandContext ctx, int max = 10)
        {
            var generatedNumber = new Random().Next(max);
            await ctx.RespondAsync($"Selected value is {generatedNumber + 1}.");
        }
    }
}
