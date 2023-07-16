using ShounenGaming.DiscordBot.Helpers;
using System.Net.Http.Headers;

namespace ShounenGaming.DiscordBot.Handlers
{
    internal class AuthHeaderHandler : DelegatingHandler
    {
        private readonly LoginHelper loginHelper;

        public AuthHeaderHandler(LoginHelper loginHelper)
        {
            this.loginHelper = loginHelper;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await loginHelper.GetToken();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {   
                await loginHelper.LoginBot();

                token = await loginHelper.GetToken();
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            } 
            
            return response;

        }
    }
}
