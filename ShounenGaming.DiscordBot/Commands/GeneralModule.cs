using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                return;
            
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
                Log.Error(ex.Message, ex);
                await ctx.RespondAsync("There was an error processing your request.");
            }
          
        }

    }
}
