using System;
using System.Threading.Tasks;
using Credfeto.Notification.Bot.Database.Interfaces;
using Credfeto.Notification.Bot.Database.Interfaces.Builders;
using Credfeto.Notification.Bot.Database.Twitch.Builders.ObjectBuilders.Entities;
using Credfeto.Notification.Bot.Database.Twitch.Builders.ObjectBuilders.Models;
using Credfeto.Notification.Bot.Twitch.Data.Interfaces;
using Credfeto.Notification.Bot.Twitch.DataTypes;

namespace Credfeto.Notification.Bot.Database.Twitch.DataManagers;

public sealed class TwitchStreamDataManager : ITwitchStreamDataManager
{
    private readonly IObjectBuilder<TwitchChatterEntity, TwitchChatter> _chatterBuilder;
    private readonly IDatabase _database;
    private readonly IObjectBuilder<TwitchFollowerEntity, TwitchFollower> _followerBuilder;
    private readonly IObjectBuilder<TwitchFollowerMilestoneEntity, TwitchFollowerMilestone> _followerMilestoneBuilder;
    private readonly IObjectBuilder<TwitchRegularChatterEntity, TwitchRegularChatter> _regularChatterBuilder;

    public TwitchStreamDataManager(IDatabase database,
                                   IObjectBuilder<TwitchChatterEntity, TwitchChatter> chatterBuilder,
                                   IObjectBuilder<TwitchRegularChatterEntity, TwitchRegularChatter> regularChatterBuilder,
                                   IObjectBuilder<TwitchFollowerMilestoneEntity, TwitchFollowerMilestone> followerMilestoneBuilder,
                                   IObjectBuilder<TwitchFollowerEntity, TwitchFollower> followerBuilder)
    {
        this._database = database ?? throw new ArgumentNullException(nameof(database));
        this._chatterBuilder = chatterBuilder ?? throw new ArgumentNullException(nameof(chatterBuilder));
        this._regularChatterBuilder = regularChatterBuilder ?? throw new ArgumentNullException(nameof(regularChatterBuilder));
        this._followerMilestoneBuilder = followerMilestoneBuilder ?? throw new ArgumentNullException(nameof(followerMilestoneBuilder));
        this._followerBuilder = followerBuilder ?? throw new ArgumentNullException(nameof(followerBuilder));
    }

    public Task RecordStreamStartAsync(Streamer streamer, DateTime streamStartDate)
    {
        return this._database.ExecuteAsync(storedProcedure: "twitch.stream_insert", new { channel_ = streamer.ToString(), start_date_ = streamStartDate });
    }

    public Task AddChatterToStreamAsync(Streamer streamer, DateTime streamStartDate, Viewer username)
    {
        return this._database.ExecuteAsync(storedProcedure: "twitch.stream_chatter_insert",
                                           new { channel_ = streamer.ToString(), start_date_ = streamStartDate, chat_user_ = username.ToString() });
    }

    public async Task<bool> IsFirstMessageInStreamAsync(Streamer streamer, DateTime streamStartDate, Viewer username)
    {
        TwitchChatter? chatted = await this._database.QuerySingleOrDefaultAsync(builder: this._chatterBuilder,
                                                                                storedProcedure: "twitch.stream_chatter_get",
                                                                                new
                                                                                {
                                                                                    channel_ = streamer.ToString(), start_date_ = streamStartDate, chat_user_ = username.ToString()
                                                                                });

        return chatted == null;
    }

    public async Task<bool> IsRegularChatterAsync(Streamer streamer, Viewer username)
    {
        TwitchRegularChatter? chatted = await this._database.QuerySingleOrDefaultAsync(builder: this._regularChatterBuilder,
                                                                                       storedProcedure: "twitch.stream_chatter_is_regular",
                                                                                       new { channel_ = streamer.ToString(), username_ = username.ToString() });

        return chatted?.Regular == true;
    }

    public async Task<bool> UpdateFollowerMilestoneAsync(Streamer streamer, int followerCount)
    {
        TwitchFollowerMilestone? milestone = await this._database.QuerySingleOrDefaultAsync(builder: this._followerMilestoneBuilder,
                                                                                            storedProcedure: "twitch.stream_milestone_insert",
                                                                                            new { channel_ = streamer.ToString(), followers_ = followerCount });

        return milestone?.FreshlyReached == true;
    }

    public async Task<int> RecordNewFollowerAsync(Streamer streamer, Viewer username)
    {
        TwitchFollower follower = await this._database.QuerySingleAsync(builder: this._followerBuilder,
                                                                        storedProcedure: "twitch.stream_follower_insert",
                                                                        new { channel_ = streamer.ToString(), follower_ = username.ToString() });

        return follower.FollowCount;
    }
}