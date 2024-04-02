using System.Collections.Generic;

namespace EpicGamesLauncher.Notifications.Models;

public class BuildNotificationsData
{
    public required List<BuildNotification> BuildNotifications { get; set; }
}
