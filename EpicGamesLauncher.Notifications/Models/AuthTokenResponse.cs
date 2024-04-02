using System;
using System.Text.Json.Serialization;

namespace EpicGamesLauncher.Notifications.Models
{
    public class AuthTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonPropertyName("expires_at")]
        public DateTime ExpiresAt { get; set; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; }
    }
}
