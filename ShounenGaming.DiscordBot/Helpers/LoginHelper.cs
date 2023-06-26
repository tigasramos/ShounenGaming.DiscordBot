using Newtonsoft.Json.Linq;
using Serilog;
using ShounenGaming.DiscordBot.Models;
using ShounenGaming.DiscordBot.Server.Services;

namespace ShounenGaming.DiscordBot.Helpers
{
    public class LoginHelper
    {
        private readonly IAuthService authService;
        private readonly AppState appState;
        private readonly ServerSettings serverSettings;

        public LoginHelper(IAuthService authService, AppState appState, AppSettings appSettings)
        {
            this.authService = authService;
            this.appState = appState;
            serverSettings = appSettings.Server;
        }

        public async Task<string> GetToken()
        {
            if (string.IsNullOrEmpty(appState.ServerToken))
                await LoginBot();

            return appState.ServerToken;
        }

        public async Task LoginBot()
        {
            try
            {
                var authResponse = await authService.LoginBot(serverSettings.BotDiscordId, serverSettings.BotPassword);
                appState.ServerToken = authResponse.AccessToken;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
        }
    }
}
