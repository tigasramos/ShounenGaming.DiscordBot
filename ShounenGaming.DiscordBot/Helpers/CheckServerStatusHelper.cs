using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShounenGaming.DiscordBot.Helpers
{
    internal class CheckServerStatusHelper
    {
        public static async Task<bool> CheckServerIsRunning(string url)
        {
            try 
            {
                var client = new HttpClient() { BaseAddress = new Uri(url) };
                var response = await client.GetAsync("healthz");
                return response.IsSuccessStatusCode && await response.Content.ReadAsStringAsync() == "Healthy";
            }
            catch
            {
                return false;
            }
        }
    }
}
