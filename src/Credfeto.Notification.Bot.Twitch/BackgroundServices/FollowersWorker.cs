using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Credfeto.Notification.Bot.Twitch.BackgroundServices;

/// <summary>
///     Live Status Background service.
/// </summary>
public sealed class FollowersWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

    private readonly ILogger<FollowersWorker> _logger;
    private readonly ITwitchFollowerDetector _twitchFollowerDetector;

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="twitchStreamStatus">Twitch Live status checks</param>
    /// <param name="logger">Logging.</param>
    /// <returns>Logging</returns>
    public FollowersWorker(ITwitchFollowerDetector twitchStreamStatus, ILogger<FollowersWorker> logger)
    {
        this._twitchFollowerDetector = twitchStreamStatus ?? throw new ArgumentNullException(nameof(twitchStreamStatus));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await this.UpdateFollowersAsync();
            await Task.Delay(delay: Interval, cancellationToken: stoppingToken);
        }
    }

    private async Task UpdateFollowersAsync()
    {
        try
        {
            await this._twitchFollowerDetector.UpdateAsync();
        }
        catch (Exception e)
        {
            this._logger.LogError(new(e.HResult), exception: e, message: "Failed to update twitch status");

            throw;
        }
    }
}