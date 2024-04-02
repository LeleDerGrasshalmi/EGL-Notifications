using System;
using System.Text.Json.Serialization;

namespace EpicGamesLauncher.Notifications.Models;

public class AuthTokenResponse
{
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; set; }

    [JsonPropertyName("expires_at")]
    public required DateTime ExpiresAt { get; set; }

    [JsonPropertyName("token_type")]
    public required string TokenType { get; set; }
}
