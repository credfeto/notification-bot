using System;
using System.Threading.Tasks;
using Credfeto.Notification.Bot.Database.Interfaces;
using Credfeto.Notification.Bot.Database.Interfaces.Builders;
using Credfeto.Notification.Bot.Database.Twitch.Builders.ObjectBuilders.Entities;
using Credfeto.Notification.Bot.Twitch.Data.Interfaces;
using Credfeto.Notification.Bot.Twitch.DataTypes;

namespace Credfeto.Notification.Bot.Database.Twitch.DataManagers;

public sealed class TwitchStreamerDataManager : ITwitchStreamerDataManager
{
    private readonly IDatabase _database;
    private readonly IObjectBuilder<TwitchUserEntity, TwitchUser> _twitchUserBuilder;

    public TwitchStreamerDataManager(IDatabase database, IObjectBuilder<TwitchUserEntity, TwitchUser> twitchUserBuilder)
    {
        this._database = database ?? throw new ArgumentNullException(nameof(database));
        this._twitchUserBuilder = twitchUserBuilder ?? throw new ArgumentNullException(nameof(twitchUserBuilder));
    }

    public Task AddStreamerAsync(Streamer streamerName, string streamerId, DateTime startedStreaming)
    {
        return this._database.ExecuteAsync(storedProcedure: "twitch.streamer_insert",
                                           new { username_ = streamerName.ToString(), id_ = streamerId, date_created_ = startedStreaming });
    }

    public Task<TwitchUser?> GetByUserNameAsync(Streamer userName)
    {
        return this._database.QuerySingleOrDefaultAsync(builder: this._twitchUserBuilder, storedProcedure: "twitch.streamer_get", new { username_ = userName.ToString() });
    }

    public Task<TwitchUser?> GetByUserNameAsync(Viewer userName)
    {
        return this._database.QuerySingleOrDefaultAsync(builder: this._twitchUserBuilder, storedProcedure: "twitch.streamer_get", new { username_ = userName.ToString() });
    }
}