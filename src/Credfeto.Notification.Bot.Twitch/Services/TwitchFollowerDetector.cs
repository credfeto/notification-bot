using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Notification.Bot.Twitch.Configuration;
using Credfeto.Notification.Bot.Twitch.DataTypes;
using Credfeto.Notification.Bot.Twitch.Extensions;
using Credfeto.Notification.Bot.Twitch.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NonBlocking;
using TwitchLib.PubSub.Events;
using TwitchLib.PubSub.Interfaces;

namespace Credfeto.Notification.Bot.Twitch.Services;

public sealed class TwitchFollowerDetector : ITwitchFollowerDetector, IDisposable
{
    private readonly IDisposable _connectedSubscription;
    private readonly IDisposable _disconnectedSubscription;
    private readonly IDisposable _followedSubscription;
    private readonly ILogger<TwitchFollowerDetector> _logger;
    private readonly TwitchBotOptions _options;
    private readonly SemaphoreSlim _semaphoreSlim;
    private readonly IDisposable _serviceErrorSubscription;
    private readonly ITwitchChannelManager _twitchChannelManager;
    private readonly ITwitchPubSub _twitchPubSub;
    private readonly ConcurrentDictionary<string, Streamer> _userMappings;

    private bool _connected;

    public TwitchFollowerDetector(IOptions<TwitchBotOptions> options,
                                  ITwitchPubSub twitchPubSub,
                                  ITwitchChannelManager twitchChannelManager,
                                  ILogger<TwitchFollowerDetector> logger)
    {
        this._options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
        this._twitchPubSub = twitchPubSub ?? throw new ArgumentNullException(nameof(twitchPubSub));
        this._twitchChannelManager = twitchChannelManager ?? throw new ArgumentNullException(nameof(twitchChannelManager));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));

        this._semaphoreSlim = new(1);
        this._userMappings = new(StringComparer.InvariantCultureIgnoreCase);
        this._connected = false;

        // FOLLOWS

        this._serviceErrorSubscription = Observable
                                         .FromEventPattern<OnPubSubServiceErrorArgs>(addHandler: h => this._twitchPubSub.OnPubSubServiceError += h,
                                                                                     removeHandler: h => this._twitchPubSub.OnPubSubServiceError -= h)
                                         .Select(messageEvent => messageEvent.EventArgs)
                                         .Subscribe(this.OnPubSubServiceError);

        this._connectedSubscription = Observable.FromEventPattern(addHandler: h => this._twitchPubSub.OnPubSubServiceConnected += h,
                                                                  removeHandler: h => this._twitchPubSub.OnPubSubServiceConnected -= h)
                                                .Select(messageEvent => messageEvent.EventArgs)
                                                .Select(_ => Observable.FromAsync(this.OnConnectedAsync))
                                                .Concat()
                                                .Subscribe();

        this._disconnectedSubscription = Observable.FromEventPattern(addHandler: h => this._twitchPubSub.OnPubSubServiceClosed += h,
                                                                     removeHandler: h => this._twitchPubSub.OnPubSubServiceClosed -= h)
                                                   .Subscribe(this.OnDisconnected);

        this._followedSubscription = Observable
                                     .FromEventPattern<OnFollowArgs>(addHandler: h => this._twitchPubSub.OnFollow += h, removeHandler: h => this._twitchPubSub.OnFollow -= h)
                                     .Select(messageEvent => messageEvent.EventArgs)
                                     .Select(e => Observable.FromAsync(cancellationToken => this.OnFollowedAsync(e: e, cancellationToken: cancellationToken)))
                                     .Concat()
                                     .Subscribe();
    }

    public void Dispose()
    {
        this._connectedSubscription.Dispose();
        this._disconnectedSubscription.Dispose();
        this._followedSubscription.Dispose();
        this._serviceErrorSubscription.Dispose();
        this._semaphoreSlim.Dispose();
    }

    public async Task EnableAsync(TwitchUser streamer)
    {
        if (this._userMappings.TryAdd(key: streamer.Id, streamer.UserName.ToStreamer()))
        {
            if (!await this.EnsureConnectedAsync())
            {
                this._logger.LogInformation($"{streamer.UserName}: Tracking follower notifications as twitch user id {streamer.Id}.");
                this._twitchPubSub.ListenToFollows(streamer.Id);
            }
        }
    }

    public Task UpdateAsync()
    {
        if (this._userMappings.IsEmpty)
        {
            return Task.CompletedTask;
        }

        return this.EnsureConnectedAsync();
    }

    private async Task<bool> EnsureConnectedAsync()
    {
        if (this._connected)
        {
            return false;
        }

        await this._semaphoreSlim.WaitAsync();

        try
        {
            if (this._connected)
            {
                return false;
            }

            this._twitchPubSub.Connect();
            await Task.Delay(TimeSpan.FromMilliseconds(value: 500));

            return true;
        }
        finally
        {
            this._semaphoreSlim.Release();
        }
    }

    private void OnPubSubServiceError(OnPubSubServiceErrorArgs e)
    {
        this._logger.LogError($"PubSub Error: {e.Exception.Message}");
    }

    private async Task OnConnectedAsync(CancellationToken cancellationToken)
    {
        this._connected = true;
        this._logger.LogInformation("PubSub Connected...");

        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken: cancellationToken);

            this._twitchPubSub.SendTopics();

            foreach ((string streamerId, Streamer streamer) in this._userMappings)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken: cancellationToken);

                this._logger.LogInformation($"{streamer}: Tracking follower notifications as twitch user id {streamerId}.");
                this._twitchPubSub.ListenToFollows(streamerId);
            }
        }
        catch (Exception exception)
        {
            this._logger.LogInformation(new(exception.HResult), exception: exception, $"PubSub: Failed to connect {exception.Message}");
        }
    }

    private void OnDisconnected(EventPattern<object> e)
    {
        this._logger.LogInformation("PubSub Disconnected...");
        this._connected = false;
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

        ITwitchChannelState state = this._twitchChannelManager.GetStreamer(channelName);

        return state.NewFollowerAsync(user: user, cancellationToken: cancellationToken);
    }
}