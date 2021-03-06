using System;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Notification.Bot.Mocks;
using Credfeto.Notification.Bot.Twitch.Actions;
using Credfeto.Notification.Bot.Twitch.DataTypes;
using Credfeto.Notification.Bot.Twitch.Models;
using Credfeto.Notification.Bot.Twitch.Publishers;
using FunFair.Test.Common;
using MediatR;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Credfeto.Notification.Bot.Twitch.Tests.Publishers;

public sealed class TwitchBitsGiftNotificationHandlerTests : TestBase
{
    private readonly IContributionThanks _contributionThanks;
    private readonly INotificationHandler<TwitchBitsGift> _notificationHandler;

    public TwitchBitsGiftNotificationHandlerTests()
    {
        this._contributionThanks = GetSubstitute<IContributionThanks>();

        this._notificationHandler = new TwitchBitsGiftNotificationHandler(contributionThanks: this._contributionThanks, this.GetTypedLogger<TwitchBitsGiftNotificationHandler>());
    }

    [Fact]
    public async Task HandleAsync()
    {
        await this._notificationHandler.Handle(new(streamer: MockReferenceData.Streamer, user: MockReferenceData.Viewer, bits: 404), cancellationToken: CancellationToken.None);

        await this.ReceivedThankForBitsAsync();
    }

    [Fact]
    public async Task HandleExceptionAsync()
    {
        this._contributionThanks.ThankForBitsAsync(Arg.Any<Streamer>(), Arg.Any<Viewer>(), Arg.Any<CancellationToken>())
            .Throws<TimeoutException>();

        await this._notificationHandler.Handle(new(streamer: MockReferenceData.Streamer, user: MockReferenceData.Viewer, bits: 404), cancellationToken: CancellationToken.None);

        await this.ReceivedThankForBitsAsync();
    }

    private Task ReceivedThankForBitsAsync()
    {
        return this._contributionThanks.Received(1)
                   .ThankForBitsAsync(streamer: MockReferenceData.Streamer, user: MockReferenceData.Viewer, Arg.Any<CancellationToken>());
    }
}