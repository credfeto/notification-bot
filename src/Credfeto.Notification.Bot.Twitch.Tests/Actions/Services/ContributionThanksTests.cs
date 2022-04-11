using System.Threading;
using System.Threading.Tasks;
using Credfeto.Notification.Bot.Shared;
using Credfeto.Notification.Bot.Twitch.Actions;
using Credfeto.Notification.Bot.Twitch.Actions.Services;
using Credfeto.Notification.Bot.Twitch.Configuration;
using Credfeto.Notification.Bot.Twitch.DataTypes;
using Credfeto.Notification.Bot.Twitch.Services;
using Credfeto.Notification.Bot.Twitch.StreamState;
using FunFair.Test.Common;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Credfeto.Notification.Bot.Twitch.Tests.Actions.Services;

public sealed class ContributionThanksTests : TestBase
{
    private static readonly Channel Channel = Types.ChannelFromString(nameof(Channel));
    private static readonly User GiftingUser = Types.UserFromString(nameof(GiftingUser));
    private static readonly User User = Types.UserFromString(nameof(User));

    private readonly IContributionThanks _contributionThanks;
    private readonly ICurrentTimeSource _currentTimeSource;
    private readonly IMessageChannel<TwitchChatMessage> _twitchChatMessageChannel;

    public ContributionThanksTests()
    {
        this._twitchChatMessageChannel = GetSubstitute<IMessageChannel<TwitchChatMessage>>();
        this._currentTimeSource = GetSubstitute<ICurrentTimeSource>();
        IOptions<TwitchBotOptions> options = Substitute.For<IOptions<TwitchBotOptions>>();
        options.Value.Returns(new TwitchBotOptions { Channels = new() { new() { ChannelName = Channel.ToString(), Thanks = new() { Enabled = true } } } });

        this._contributionThanks = new ContributionThanks(options: options,
                                                          twitchChatMessageChannel: this._twitchChatMessageChannel,
                                                          currentTimeSource: this._currentTimeSource,
                                                          this.GetTypedLogger<ContributionThanks>());
    }

    private ValueTask ReceivedPublishMessageAsync(string expectedMessage)
    {
        return this._twitchChatMessageChannel.Received(1)
                   .PublishAsync(Arg.Is<TwitchChatMessage>(t => t.Channel == Channel && t.Message == expectedMessage), Arg.Any<CancellationToken>());
    }

    private ValueTask DidNotReceivePublishMessageAsync()
    {
        return this._twitchChatMessageChannel.DidNotReceive()
                   .PublishAsync(Arg.Any<TwitchChatMessage>(), Arg.Any<CancellationToken>());
    }

    private void ReceivedCurrentTime()
    {
        this._currentTimeSource.Received(1)
            .UtcNow();
    }

    private void DidNotReceiveCurrentTime()
    {
        this._currentTimeSource.DidNotReceive()
            .UtcNow();
    }

    [Fact]
    public async Task ThanksForBitsAsync()
    {
        await this._contributionThanks.ThankForBitsAsync(channel: Channel, user: GiftingUser, cancellationToken: CancellationToken.None);

        await this.ReceivedPublishMessageAsync($"Thanks @{GiftingUser} for the bits");

        this.DidNotReceiveCurrentTime();
    }

    [Fact]
    public async Task ThankForGiftingOneSubAsync()
    {
        await this._contributionThanks.ThankForGiftingSubAsync(channel: Channel, giftedBy: GiftingUser, cancellationToken: CancellationToken.None);

        await this.ReceivedPublishMessageAsync($"Thanks @{GiftingUser} for gifting sub");

        this.ReceivedCurrentTime();
    }

    [Fact]
    public async Task ThankForGiftingMultipleSubsAsync()
    {
        await this._contributionThanks.ThankForMultipleGiftSubsAsync(channel: Channel, giftedBy: GiftingUser, count: 27, cancellationToken: CancellationToken.None);

        await this.ReceivedPublishMessageAsync($"Thanks @{GiftingUser} for gifting subs");

        this.ReceivedCurrentTime();
    }

    [Fact]
    public async Task ThankForNewPaidSubAsync()
    {
        await this._contributionThanks.ThankForNewPaidSubAsync(channel: Channel, user: User, cancellationToken: CancellationToken.None);

        await this.ReceivedPublishMessageAsync($"Thanks @{User} for subscribing");

        this.DidNotReceiveCurrentTime();
    }

    [Fact]
    public async Task ThankForNewPrimeSubAsync()
    {
        await this._contributionThanks.ThankForNewPrimeSubAsync(channel: Channel, user: User, cancellationToken: CancellationToken.None);

        await this.ReceivedPublishMessageAsync($"Thanks @{User} for subscribing");

        this.DidNotReceiveCurrentTime();
    }

    [Fact]
    public async Task ThankForPaidReSubAsync()
    {
        await this._contributionThanks.ThankForPaidReSubAsync(channel: Channel, user: User, cancellationToken: CancellationToken.None);

        await this.ReceivedPublishMessageAsync($"Thanks @{User} for resubscribing");

        this.DidNotReceiveCurrentTime();
    }

    [Fact]
    public async Task ThankForPrimeReSubAsync()
    {
        await this._contributionThanks.ThankForPrimeReSubAsync(channel: Channel, user: User, cancellationToken: CancellationToken.None);

        await this.ReceivedPublishMessageAsync($"Thanks @{User} for resubscribing");

        this.DidNotReceiveCurrentTime();
    }

    [Fact]
    public async Task ThankForFollowAsync()
    {
        await this._contributionThanks.ThankForFollowAsync(channel: Channel, user: User, cancellationToken: CancellationToken.None);

        await this.DidNotReceivePublishMessageAsync();

        this.DidNotReceiveCurrentTime();
    }
}