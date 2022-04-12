using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Notification.Bot.Twitch.Configuration;
using Credfeto.Notification.Bot.Twitch.Data.Interfaces;
using Credfeto.Notification.Bot.Twitch.DataTypes;
using Credfeto.Notification.Bot.Twitch.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NonBlocking;
using TwitchLib.PubSub.Events;
using TwitchLib.PubSub.Interfaces;

namespace Credfeto.Notification.Bot.Twitch.Services;

public sealed class TwitchFollowerDetector : ITwitchFollowerDetector
{
    private readonly ILogger<TwitchFollowerDetector> _logger;
    private readonly TwitchBotOptions _options;
    private readonly ITwitchChannelManager _twitchChannelManager;
    private readonly ITwitchPubSub _twitchPubSub;
    private readonly ConcurrentDictionary<string, Streamer> _userMappings;

    public TwitchFollowerDetector(IOptions<TwitchBotOptions> options, ITwitchPubSub twitchPubSub, ITwitchChannelManager twitchChannelManager, ILogger<TwitchFollowerDetector> logger)
    {
        this._options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
        this._twitchPubSub = twitchPubSub ?? throw new ArgumentNullException(nameof(twitchPubSub));
        this._twitchChannelManager = twitchChannelManager ?? throw new ArgumentNullException(nameof(twitchChannelManager));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));

        this._userMappings = new(StringComparer.InvariantCultureIgnoreCase);

        // FOLLOWS

        Observable.FromEventPattern<OnPubSubServiceErrorArgs>(addHandler: h => this._twitchPubSub.OnPubSubServiceError += h, removeHandler: h => this._twitchPubSub.OnPubSubServiceError -= h)
                  .Select(messageEvent => messageEvent.EventArgs)
                  .Subscribe(this.OnPubSubServiceError);

        Observable.FromEventPattern(addHandler: h => this._twitchPubSub.OnPubSubServiceConnected += h, removeHandler: h => this._twitchPubSub.OnPubSubServiceConnected -= h)
                  .Subscribe(this.OnPubSubServiceConnected);

        Observable.FromEventPattern<OnFollowArgs>(addHandler: h => this._twitchPubSub.OnFollow += h, removeHandler: h => this._twitchPubSub.OnFollow -= h)
                  .Select(messageEvent => messageEvent.EventArgs)
                  .Select(e => Observable.FromAsync(cancellationToken => this.OnFollowedAsync(e: e, cancellationToken: cancellationToken)))
                  .Concat()
                  .Subscribe();

        this._twitchPubSub.Connect();
        this._twitchPubSub.SendTopics();
    }

    public void Enable(TwitchUser streamer)
    {
        if (this._userMappings.TryAdd(key: streamer.Id, streamer.UserName.ToStreamer()))
        {
            this._logger.LogInformation($"{streamer.UserName}: Tracking follower notifications as twitch user id {streamer.Id}.");
            this._twitchPubSub.ListenToFollows(streamer.Id);
        }
    }

    public Task UpdateAsync()
    {
        return Task.CompletedTask;
    }

    private void OnPubSubServiceError(OnPubSubServiceErrorArgs e)
    {
        this._logger.LogError($"{e.Exception.Message}");
    }

    private void OnPubSubServiceConnected(EventPattern<object> e)
    {
        this._logger.LogInformation("PubSub Connected");
    }

    private Task OnFollowedAsync(OnFollowArgs e, in CancellationToken cancellationToken)
    {
        if (!this._userMappings.TryGetValue(key: e.FollowedChannelId, out Streamer channelName))
        {
            return Task.CompletedTask;
        }

        Viewer user = Viewer.FromString(e.Username);

        this._logger.LogInformation($"{channelName}: (Id: {e.FollowedChannelId}) Followed by {user}");

        if (!this._options.IsModChannel(channelName))
        {
            return Task.CompletedTask;
        }

        ITwitchChannelState state = this._twitchChannelManager.GetChannel(channelName);

        return state.NewFollowerAsync(user: user, cancellationToken: cancellationToken);
    }
}