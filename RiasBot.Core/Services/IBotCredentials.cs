using System;
using System.Collections.Generic;
using System.Text;

namespace RiasBot.Services
{
    public interface IBotCredentials
    {
        ulong ClientId { get; }
        string Prefix { get; }
        string Token { get; }
        string GoogleApiKey { get; }
        string UrbanDictionaryApiKey { get; }
        string PatreonAccessToken { get; }
        string DiscordBotsListApiKey { get; }
        string ImgurClientID { get; } //this client id is from Koneko who manage the lists of reactions images
        string HelpDM { get; }
    }
}
