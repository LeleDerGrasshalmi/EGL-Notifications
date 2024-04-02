using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EpicGamesLauncher.Notifications.Models;

public class BuildNotification
{
    public required string NotificationId { get; set; }
    public required string DisplayCondition { get; set; }
    public required string LayoutPath { get; set; }
    public required string DismissId { get; set; }
    public required string ImagePath { get; set; }
    public required string UriLink { get; set; }

    public required bool IsAdvert { get; set; }
    public required bool IsFreeGame { get; set; }

    public required string Title { get; set; }
    public required string Description { get; set; }
    public required List<string> AccountCountryBlackList { get; set; }

    [JsonExtensionData]
    public IDictionary<string, object> Extensions { get; set; } = new Dictionary<string, object>();
}
