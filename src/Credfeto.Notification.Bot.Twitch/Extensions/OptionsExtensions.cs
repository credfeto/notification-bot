using System;
using System.Linq;
using Credfeto.Notification.Bot.Twitch.Configuration;
using Credfeto.Notification.Bot.Twitch.DataTypes;
using TwitchLib.Api;

namespace Credfeto.Notification.Bot.Twitch.Extensions;

internal static class OptionsExtensions
{
    public static TwitchModChannel? GetModChannel(this TwitchBotOptions options, Streamer streamer)
    {
        return options.Channels.Find(c => StringComparer.InvariantCultureIgnoreCase.Equals(x: c.ChannelName, y: streamer.Value));
    }

    public static bool IsModChannel(this TwitchBotOptions options, in Streamer streamer)
    {
        return options.GetModChannel(streamer) != null;
    }

    public static TwitchAPI ConfigureTwitchApi(this TwitchBotOptions options)
    {
        return new()
               {
                   Settings =
                   {
                       ClientId = options.Authentication.ClientId, Secret = options.Authentication.ClientSecret
                       /*
                       , AccessToken = options.Authentication.ClientAccessToken */
                   }
               };
    }

    public static bool IsIgnoredUser(this TwitchBotOptions options, Viewer username)
    {
        return options.IgnoredUsers.Any(c => StringComparer.InvariantCultureIgnoreCase.Equals(x: c, y: username.Value));
    }

    public static bool IsSelf(this TwitchBotOptions options, in Viewer username)
    {
        return StringComparer.InvariantCultureIgnoreCase.Equals(x: options.Authentication.UserName, y: username.Value);
    }
}