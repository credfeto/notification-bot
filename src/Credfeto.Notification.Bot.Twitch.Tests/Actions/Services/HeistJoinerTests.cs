using System.Threading;
using System.Threading.Tasks;
using Credfeto.Notification.Bot.Shared;
using Credfeto.Notification.Bot.Twitch.Actions;
using Credfeto.Notification.Bot.Twitch.Actions.Services;
using Credfeto.Notification.Bot.Twitch.StreamState;
using FunFair.Test.Common;
using NSubstitute;
using Xunit;

namespace Credfeto.Notification.Bot.Twitch.Tests.Actions.Services;

public sealed class HeistJoinerTests : TestBase
{
    private const string CHANNEL = nameof(CHANNEL);
    private readonly IHeistJoiner _heistJoiner;
    private readonly IMessageChannel<TwitchChatMessage> _twitchChatMessageChannel;

    public HeistJoinerTests()
    {
        this._twitchChatMessageChannel = GetSubstitute<IMessageChannel<TwitchChatMessage>>();

        this._heistJoiner = new HeistJoiner(twitchChatMessageChannel: this._twitchChatMessageChannel, this.GetTypedLogger<HeistJoiner>());
    }

    [Fact]
    public async Task JoinHeistAsync()
    {
        await this._heistJoiner.JoinHeistAsync(channel: CHANNEL, cancellationToken: CancellationToken.None);

        await this.ReceivedPublishMessageAsync("!heist all");
    }

    private ValueTask ReceivedPublishMessageAsync(string expectedMessage)
    {
        return this._twitchChatMessageChannel.Received(1)
                   .PublishAsync(Arg.Is<TwitchChatMessage>(t => t.Channel == CHANNEL && t.Message == expectedMessage), Arg.Any<CancellationToken>());
    }
}