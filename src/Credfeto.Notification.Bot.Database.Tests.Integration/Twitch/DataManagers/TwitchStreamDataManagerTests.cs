using System;
using System.Threading.Tasks;
using Credfeto.Notification.Bot.Database.Tests.Integration.Setup;
using Credfeto.Notification.Bot.Shared;
using Credfeto.Notification.Bot.Twitch.Data.Interfaces;
using Xunit;
using Xunit.Abstractions;

namespace Credfeto.Notification.Bot.Database.Tests.Integration.Twitch.DataManagers;

public sealed class TwitchStreamDataManagerTests : DatabaseIntegrationTestBase
{
    private readonly ICurrentTimeSource _currentTimeSource;
    private readonly ITwitchStreamDataManager _twitchStreamDataManager;

    public TwitchStreamDataManagerTests(ITestOutputHelper output)
        : base(output)
    {
        this._twitchStreamDataManager = this.GetService<ITwitchStreamDataManager>();
        this._currentTimeSource = this.GetService<ICurrentTimeSource>();
    }

    [Fact]
    public Task AddAsync()
    {
        string channelName = "@" + Guid.NewGuid()
                                       .ToString()
                                       .Replace(oldValue: "-", newValue: "");

        return this._twitchStreamDataManager.RecordStreamStartAsync(channel: channelName, this._currentTimeSource.UtcNow());
    }
}