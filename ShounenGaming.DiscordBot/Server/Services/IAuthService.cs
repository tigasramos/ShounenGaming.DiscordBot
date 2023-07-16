using Refit;
using ShounenGaming.DiscordBot.Server.Models;

namespace ShounenGaming.DiscordBot.Server.Services
{
    public interface IAuthService
    {
        [Get("/auth/bot/login")]
        Task<AuthResponse> LoginBot([Query]string discordId, [Query]string password);


        [Post("/auth/user")]
        Task RegisterUser([Body]CreateUser createUser);
    }
}
