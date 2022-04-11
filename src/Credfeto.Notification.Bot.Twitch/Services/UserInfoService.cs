using System;
using System.Threading.Tasks;
using Credfeto.Notification.Bot.Twitch.Configuration;
using Credfeto.Notification.Bot.Twitch.Data.Interfaces;
using Credfeto.Notification.Bot.Twitch.DataTypes;
using Credfeto.Notification.Bot.Twitch.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NonBlocking;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using User = Credfeto.Notification.Bot.Twitch.DataTypes.User;

namespace Credfeto.Notification.Bot.Twitch.Services;

public sealed class UserInfoService : IUserInfoService
{
    private readonly TwitchAPI _api;

    private readonly ConcurrentDictionary<User, TwitchUser?> _cache;
    private readonly ILogger<UserInfoService> _logger;
    private readonly TwitchBotOptions _options;
    private readonly ITwitchStreamerDataManager _twitchStreamerDataManager;

    public UserInfoService(IOptions<TwitchBotOptions> options, ITwitchStreamerDataManager twitchStreamerDataManager, ILogger<UserInfoService> logger)
    {
        this._twitchStreamerDataManager = twitchStreamerDataManager ?? throw new ArgumentNullException(nameof(twitchStreamerDataManager));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this._options = (options ?? throw new ArgumentNullException(nameof(options))).Value;

        this._api = this._options.ConfigureTwitchApi();

        this._cache = new();
    }

    public Task<TwitchUser?> GetUserAsync(Channel userName)
    {
        return this.GetUserAsync(userName.UserFromChannel());
    }

    public async Task<TwitchUser?> GetUserAsync(User userName)
    {
        if (this._cache.TryGetValue(key: userName, out TwitchUser? user))
        {
            return user;
        }

        user = await this._twitchStreamerDataManager.GetByUserNameAsync(userName);

        if (user != null)
        {
            this._cache.TryAdd(key: userName, value: user);

            return user;
        }

        try
        {
            this._logger.LogDebug($"Getting User information for {userName}");
            GetUsersResponse result = await this._api.Helix.Users.GetUsersAsync(logins: new() { userName.ToString() });

            if (result.Users.Length == 0)
            {
                this._cache.TryAdd(key: userName, value: null);

                return null;
            }

            user = Convert(result.Users[0]);
            this._cache.TryAdd(key: userName, value: user);

            if (user.IsStreamer)
            {
                await this._twitchStreamerDataManager.AddStreamerAsync(new(user.UserName.ToString()), streamerId: user.Id, startedStreaming: user.DateCreated);
            }

            return user;
        }
        catch (Exception exception)
        {
            this._logger.LogError(new(exception.HResult), exception: exception, $"Failed to look up user information for {userName}: {exception.Message}");

            return null;
        }
    }

    private static TwitchUser Convert(TwitchLib.Api.Helix.Models.Users.GetUsers.User user)
    {
        return new(userName: new(user.Login.ToLowerInvariant()), id: user.Id, isStreamer: !string.IsNullOrWhiteSpace(user.BroadcasterType), dateCreated: user.CreatedAt);
    }
}