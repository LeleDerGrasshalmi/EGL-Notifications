using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EpicGamesLauncher.Notifications.Models;

public class FormattedNotification
{
    public required string Id { get; set; }
    public required string Condition { get; set; }
    public required string Layout { get; set; }
    public required string Image { get; set; }
    public required string Link { get; set; }

    public required Dictionary<string, string> Title { get; set; }
    public required Dictionary<string, string> Description { get; set; }

    [JsonExtensionData]
    public IDictionary<string, object> Extensions { get; set; } = new Dictionary<string, object>();

    public static readonly List<string> BlacklistedExtensions = new List<string>
    {
        "NotificationId",
        "DisplayCondition",
        "LayoutPath",
        "DismissId",
        "ImagePath",
        "UriLink",
    };
}
