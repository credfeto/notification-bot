using System;

namespace Credfeto.Notification.Bot.Database.Twitch.Builders.ObjectBuilders.Entities;

public sealed class TwitchUserEntity
{
    public string? Id { get; init; }

    public string? UserName { get; init; }

    public DateTime DateCreated { get; init; }
}