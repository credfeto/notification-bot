using System;
using Credfeto.Notification.Bot.Twitch.Actions;
using Credfeto.Notification.Bot.Twitch.Configuration;
using Credfeto.Notification.Bot.Twitch.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NonBlocking;

namespace Credfeto.Notification.Bot.Twitch.Services;

public sealed class TwitchChannelManager : ITwitchChannelManager
{
    private readonly ILogger<TwitchChannelManager> _logger;
    private readonly TwitchBotOptions _options;
    private readonly IRaidWelcome _raidWelcome;
    private readonly IShoutoutJoiner _shoutoutJoiner;
    private readonly ConcurrentDictionary<string, TwitchChannelState> _streamStates;

    public TwitchChannelManager(IOptions<TwitchBotOptions> options, IRaidWelcome raidWelcome, IShoutoutJoiner shoutoutJoiner, ILogger<TwitchChannelManager> logger)
    {
        this._raidWelcome = raidWelcome ?? throw new ArgumentNullException(nameof(raidWelcome));
        this._shoutoutJoiner = shoutoutJoiner ?? throw new ArgumentNullException(nameof(shoutoutJoiner));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this._streamStates = new(comparer: StringComparer.OrdinalIgnoreCase);
        this._options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
    }

    /// <inheritdoc />
    public TwitchChannelState GetChannel(string channel)
    {
        if (this._streamStates.TryGetValue(key: channel, out TwitchChannelState? state))
        {
            return state;
        }

        return this._streamStates.GetOrAdd(key: channel,
                                           new TwitchChannelState(channel.ToLowerInvariant(),
                                                                  options: this._options,
                                                                  raidWelcome: this._raidWelcome,
                                                                  shoutoutJoiner: this._shoutoutJoiner,
                                                                  logger: this._logger));
    }
}