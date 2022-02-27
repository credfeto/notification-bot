using System;
using System.Linq;
using Credfeto.Notification.Bot.Twitch.Configuration;

namespace Credfeto.Notification.Bot.Twitch.Extensions;

internal static class OptionsExtensions
{
    public static bool IsModChannel(this TwitchBotOptions options, string channel)
    {
        return options.Channels.Any(c => StringComparer.InvariantCultureIgnoreCase.Equals(x: c, y: channel));
    }

    public static bool RaidWelcomeEnabled(this TwitchBotOptions options, string channel)
    {
        return options.IsModChannel(channel) && options.Raids.Any(c => StringComparer.InvariantCultureIgnoreCase.Equals(x: c, y: channel));
    }
}