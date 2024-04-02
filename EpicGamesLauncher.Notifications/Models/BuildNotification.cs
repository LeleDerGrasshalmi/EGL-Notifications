using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EpicGamesLauncher.Notifications.Models;

public class BuildNotification : Dictionary<string, JsonElement>
{
    public string NotificationId => this[nameof(NotificationId)].GetString()!;
    public string DisplayCondition => this[nameof(DisplayCondition)].GetString()!;
    public string LayoutPath => this[nameof(LayoutPath)].GetString()!;
    public string DismissId => this[nameof(DismissId)].GetString()!;
    public string ImagePath => this[nameof(ImagePath)].GetString()!;
    public string UriLink => this[nameof(UriLink)].GetString()!;

    public bool IsAdvert => this[nameof(IsAdvert)].GetBoolean();    
    public bool IsFreeGame => this[nameof(IsFreeGame)].GetBoolean();

    [JsonExtensionData]
    public IDictionary<string, object> Extensions { get; set; } = new Dictionary<string, object>();
}
