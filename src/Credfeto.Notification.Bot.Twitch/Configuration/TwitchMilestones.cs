using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Credfeto.Notification.Bot.Twitch.Configuration;

public sealed class TwitchMilestones
{
    [JsonConstructor]
    public TwitchMilestones(List<int> followers, List<int> subscribers)
    {
        this.Followers = followers;
        this.Subscribers = subscribers;
    }

    public List<int> Followers { get; }

    public List<int> Subscribers { get; }
}