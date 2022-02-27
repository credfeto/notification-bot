using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Credfeto.Notification.Bot.Twitch.BackgroundServices;

/// <summary>
///     Background service.
/// </summary>
public sealed class UpdateTwitchLiveStatusWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

    private readonly ILogger<UpdateTwitchLiveStatusWorker> _logger;
    private readonly ITwitchStreamStatus _twitchStreamStatus;

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="twitchStreamStatus">Twitch Live status checks</param>
    /// <param name="logger">Logging.</param>
    /// <returns>Logging</returns>
    public UpdateTwitchLiveStatusWorker(ITwitchStreamStatus twitchStreamStatus, ILogger<UpdateTwitchLiveStatusWorker> logger)
    {
        this._twitchStreamStatus = twitchStreamStatus ?? throw new ArgumentNullException(nameof(twitchStreamStatus));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await this.UpdateStatusAsync();
            await Task.Delay(delay: Interval, cancellationToken: stoppingToken);
        }
    }

    private async Task UpdateStatusAsync()
    {
        try
        {
            await this._twitchStreamStatus.UpdateAsync();
        }
        catch (Exception e)
        {
            this._logger.LogError(new(e.HResult), exception: e, message: "Failed to update twitch status");

            throw;
        }
    }
}