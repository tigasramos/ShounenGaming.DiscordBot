using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShounenGaming.DiscordBot.Commands
{
    [ModuleLifespan(ModuleLifespan.Transient)]
    public class BasicCommandModule : BaseCommandModule
    {
        //The line below is received by DI
        //public Random Rng { private get; set; } // Implied public setter.

        [Command("greet")]
        public async Task GreetCommand(CommandContext ctx)
        {
            await ctx.RespondAsync("Greetings! Thank you for executing me!");
        }
    }
}
