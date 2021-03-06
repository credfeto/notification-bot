using System.Threading;
using System.Threading.Tasks;
using Credfeto.Notification.Bot.Mocks;
using Credfeto.Notification.Bot.Shared;
using Credfeto.Notification.Bot.Twitch.Actions;
using Credfeto.Notification.Bot.Twitch.Actions.Services;
using Credfeto.Notification.Bot.Twitch.Interfaces;
using Credfeto.Notification.Bot.Twitch.StreamState;
using FunFair.Test.Common;
using NSubstitute;
using Xunit;

namespace Credfeto.Notification.Bot.Twitch.Tests.Actions.Services;

public sealed class WelcomeWaggonTests : TestBase
{
    private readonly ITwitchChannelState _twitchChannelState;
    private readonly IMessageChannel<TwitchChatMessage> _twitchChatMessageChannel;
    private readonly IWelcomeWaggon _welcomeWaggon;

    public WelcomeWaggonTests()
    {
        this._twitchChatMessageChannel = GetSubstitute<IMessageChannel<TwitchChatMessage>>();

        ITwitchChannelManager twitchChannelManager = GetSubstitute<ITwitchChannelManager>();

        this._twitchChannelState = GetSubstitute<ITwitchChannelState>();
        twitchChannelManager.GetStreamer(MockReferenceData.Streamer)
                            .Returns(this._twitchChannelState);

        this._welcomeWaggon = new WelcomeWaggon(twitchChannelManager: twitchChannelManager,
                                                twitchChatMessageChannel: this._twitchChatMessageChannel,
                                                this.GetTypedLogger<WelcomeWaggon>());
    }

    [Fact]
    public async Task WelcomeAsync()
    {
        this._twitchChannelState.Settings.ChatWelcomesEnabled.Returns(true);

        await this._welcomeWaggon.IssueWelcomeAsync(streamer: MockReferenceData.Streamer, user: MockReferenceData.Viewer, cancellationToken: CancellationToken.None);

        await this.ReceivedPublishMessageAsync($"Hi @{MockReferenceData.Viewer}");
    }

    [Fact]
    public async Task NotWelcomeWhenWelcomesDisabledAsync()
    {
        this._twitchChannelState.Settings.ChatWelcomesEnabled.Returns(false);

        await this._welcomeWaggon.IssueWelcomeAsync(streamer: MockReferenceData.Streamer, user: MockReferenceData.Viewer, cancellationToken: CancellationToken.None);

        await this.DidNotReceivePublishMessageAsync();
    }

    private ValueTask ReceivedPublishMessageAsync(string expectedMessage)
    {
        return this._twitchChatMessageChannel.Received(1)
                   .PublishAsync(Arg.Is<TwitchChatMessage>(t => t.Streamer == MockReferenceData.Streamer && t.Message == expectedMessage), Arg.Any<CancellationToken>());
    }

    private ValueTask DidNotReceivePublishMessageAsync()
    {
        return this._twitchChatMessageChannel.DidNotReceive()
                   .PublishAsync(Arg.Any<TwitchChatMessage>(), Arg.Any<CancellationToken>());
    }
}