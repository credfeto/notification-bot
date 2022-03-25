using System;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Notification.Bot.Shared;
using Credfeto.Notification.Bot.Twitch.StreamState;
using Microsoft.Extensions.Logging;
using NonBlocking;

namespace Credfeto.Notification.Bot.Twitch.Actions.Services;

public sealed class ContributionThanks : MessageSenderBase, IContributionThanks
{
    private readonly ICurrentTimeSource _currentTimeSource;
    private readonly SemaphoreSlim _gifterLock;

    private readonly ConcurrentDictionary<string, SubGifter> _gifters;
    private readonly ILogger<ContributionThanks> _logger;

    public ContributionThanks(IMessageChannel<TwitchChatMessage> twitchChatMessageChannel, ICurrentTimeSource currentTimeSource, ILogger<ContributionThanks> logger)
        : base(twitchChatMessageChannel)
    {
        this._currentTimeSource = currentTimeSource ?? throw new ArgumentNullException(nameof(currentTimeSource));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));

        this._gifters = new(StringComparer.CurrentCultureIgnoreCase);
        this._gifterLock = new(1);
    }

    public async Task ThankForBitsAsync(string channel, string user, CancellationToken cancellationToken)
    {
        await this.SendMessageAsync(channel: channel, $"Thanks @{user} for the bits.", cancellationToken: cancellationToken);

        this._logger.LogInformation($"{channel}: Thanks @{user} for for the bits.");
    }

    public async Task ThankForNewPrimeSubAsync(string channel, string user, CancellationToken cancellationToken)
    {
        await this.SendMessageAsync(channel: channel, $"Thanks @{user} for subscribing", cancellationToken: cancellationToken);

        this._logger.LogInformation($"{channel}: Thanks @{user} for subscribing (Prime)");
    }

    public async Task ThankForPrimeReSubAsync(string channel, string user, CancellationToken cancellationToken)
    {
        await this.SendMessageAsync(channel: channel, $"Thanks @{user} for resubscribing", cancellationToken: cancellationToken);

        this._logger.LogInformation($"{channel}: Thanks @{user} for resubscribing (Prime)");
    }

    public async Task ThankForPaidReSubAsync(string channel, string user, CancellationToken cancellationToken)
    {
        await this.SendMessageAsync(channel: channel, $"Thanks @{user} for resubscribing", cancellationToken: cancellationToken);

        this._logger.LogInformation($"{channel}: Thanks @{user} for resubscribing (Paid)");
    }

    public async Task ThankForNewPaidSubAsync(string channel, string user, CancellationToken cancellationToken)
    {
        await this.SendMessageAsync(channel: channel, $"Thanks @{user} for subscribing", cancellationToken: cancellationToken);

        this._logger.LogInformation($"{channel}: Thanks @{user} for subscribing (Paid)");
    }

    public async Task ThankForMultipleGiftSubsAsync(string channelName, string giftedBy, int count, CancellationToken cancellationToken)
    {
        if (await this.WasLastGifterAsync(channel: channelName, giftedBy: giftedBy))
        {
            this._logger.LogInformation($"{channelName}: Thanks @{giftedBy} for gifting sub (Same as last gifter).");

            return;
        }

        await this.SendMessageAsync(channel: channelName, $"Thanks @{giftedBy} for gifting subs.", cancellationToken: cancellationToken);

        this._logger.LogInformation($"{channelName}: Thanks @{giftedBy} for gifting subs.");
    }

    public async Task ThankForGiftingSubAsync(string channelName, string giftedBy, CancellationToken cancellationToken)
    {
#if FALSE
 steveforward: Community Sub: Lou_Vella
 steveforward: Thanks Lou_Vella for gifting subs.
 steveforward: jimminewtron666: hell yeah! stevef18Woah stevef18Hornsofrock stevef18Woah stevef18Hornsofrock stevef18Woah stevef18Hornsofrock stevef18Woah stevef18Hornsofrock stevef18Woah stevef18Hornsofrock stevef18Woah stevef18Hornsofrock philsmLIT philsmLIT philsmLIT philsmLIT philsmLIT philsmLIT
 steveforward: Community Sub: Lou_Vella
 steveforward: Thanks Lou_Vella for gifting sub.
 steveforward: Community Sub: Lou_Vella
 steveforward: Thanks Lou_Vella for gifting sub.
 steveforward: Community Sub: Lou_Vella
 steveforward: Thanks Lou_Vella for gifting sub.
 steveforward: Community Sub: Lou_Vella
 steveforward: Thanks Lou_Vella for gifting sub.
 steveforward: Community Sub: Lou_Vella
 steveforward: Thanks Lou_Vella for gifting sub.
 steveforward: Community Sub: Lou_Vella
 steveforward: Thanks Lou_Vella for gifting sub.
 steveforward: Community Sub: Lou_Vella
 steveforward: Thanks Lou_Vella for gifting sub.
 steveforward: Community Sub: Lou_Vella
 steveforward: Thanks Lou_Vella for gifting sub.
 steveforward: Community Sub: Lou_Vella
 steveforward: Thanks Lou_Vella for gifting sub.
 steveforward: lou_vella: lfsHH lfsHH lfsHH lfsHH lfsHH lfsHH lfsHH lfsHH
 steveforward: Community Sub: Lou_Vella
 steveforward: Thanks Lou_Vella for gifting sub.

[00:07:29 INF] steveforward: Community Sub: music_fanatic101
[00:07:29 INF] steveforward: Thanks music_fanatic101 for gifting subs.
[00:07:31 INF] steveforward: Community Sub: music_fanatic101
[00:07:31 INF] steveforward: Thanks music_fanatic101 for gifting sub.
[00:07:31 INF] steveforward: Community Sub: music_fanatic101
[00:07:31 INF] steveforward: Thanks music_fanatic101 for gifting sub.
[00:07:31 INF] steveforward: Community Sub: music_fanatic101
[00:07:31 INF] steveforward: Thanks music_fanatic101 for gifting sub.
[00:07:31 INF] steveforward: Community Sub: music_fanatic101
[00:07:31 INF] steveforward: Thanks music_fanatic101 for gifting sub.
[00:07:31 INF] steveforward: Community Sub: music_fanatic101
[00:07:31 INF] steveforward: Thanks music_fanatic101 for gifting sub.
[00:07:31 INF] steveforward: Community Sub: music_fanatic101
[00:07:31 INF] steveforward: Thanks music_fanatic101 for gifting sub.
[00:07:31 INF] steveforward: Community Sub: music_fanatic101
[00:07:31 INF] steveforward: Thanks music_fanatic101 for gifting sub.
[00:07:31 INF] steveforward: Community Sub: music_fanatic101
[00:07:31 INF] steveforward: Thanks music_fanatic101 for gifting sub.
[00:07:31 INF] steveforward: Community Sub: music_fanatic101
[00:07:31 INF] steveforward: Thanks music_fanatic101 for gifting sub.
[00:07:32 INF] steveforward: Community Sub: music_fanatic101
[00:07:32 INF] steveforward: Thanks music_fanatic101 for gifting sub.

#endif
        if (await this.WasLastGifterAsync(channel: channelName, giftedBy: giftedBy))
        {
            this._logger.LogInformation($"{channelName}: Thanks @{giftedBy} for gifting sub (Same as last gifter).");

            return;
        }

        await this.SendMessageAsync(channel: channelName, $"Thanks @{giftedBy} for gifting sub.", cancellationToken: cancellationToken);

        this._logger.LogInformation($"{channelName}: Thanks @{giftedBy} for gifting sub.");
    }

    public Task ThankForFollowAsync(string channelName, string user, CancellationToken cancellationToken)
    {
        this._logger.LogInformation($"{channelName}: Thanks @{user} for following.");

        return Task.CompletedTask;
    }

    private async Task<bool> WasLastGifterAsync(string channel, string giftedBy)
    {
        await this._gifterLock.WaitAsync();

        try
        {
            SubGifter subGifter = this.GetSubGifter(channel);

            return subGifter.Update(giftedBy);
        }
        finally
        {
            this._gifterLock.Release();
        }
    }

    private SubGifter GetSubGifter(string channel)
    {
        if (this._gifters.TryGetValue(key: channel, out SubGifter? gifter))
        {
            return gifter;
        }

        gifter = new(Guid.NewGuid()
                         .ToString(),
                     currentTimeSource: this._currentTimeSource,
                     logger: this._logger);

        return this._gifters.GetOrAdd(key: channel, value: gifter);
    }
}