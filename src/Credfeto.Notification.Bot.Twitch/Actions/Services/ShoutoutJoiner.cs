using System;
using System.Threading;
using System.Threading.Tasks;
using Credfeto.Notification.Bot.Shared;
using Credfeto.Notification.Bot.Twitch.Configuration;
using Credfeto.Notification.Bot.Twitch.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Credfeto.Notification.Bot.Twitch.Actions.Services;

public sealed class ShoutoutJoiner : MessageSenderBase, IShoutoutJoiner
{
    private readonly ILogger<ShoutoutJoiner> _logger;
    private readonly TwitchBotOptions _options;

    public ShoutoutJoiner(IOptions<TwitchBotOptions> options, IMessageChannel<TwitchChatMessage> twitchChatMessageChannel, ILogger<ShoutoutJoiner> logger)
        : base(twitchChatMessageChannel)
    {
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this._options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
    }

    public async Task IssueShoutoutAsync(string channel, string visitingStreamer, CancellationToken cancellationToken)
    {
        this._logger.LogInformation($"{channel}: Checking if need to shoutout {visitingStreamer}");
        TwitchChannelShoutout? soChannel = this._options.Shoutouts.Find(c => StringComparer.InvariantCultureIgnoreCase.Equals(x: c.Channel, y: channel));

        if (soChannel == null)
        {
            return;
        }

        TwitchFriendChannel? streamer = soChannel.FriendChannels.Find(c => StringComparer.InvariantCultureIgnoreCase.Equals(x: c.Channel, y: visitingStreamer));

        if (streamer == null)
        {
            // TODO: Check to see if the user has streamed recently using API
            // TODO: Can mod comments be read?
            return;
        }

        if (string.IsNullOrWhiteSpace(streamer.Message))
        {
            await this.SendMessageAsync(channel: channel, $"Check out https://www.twitch.tv/{visitingStreamer}", cancellationToken: cancellationToken);
        }
        else
        {
            await this.SendMessageAsync(channel: channel, message: streamer.Message, cancellationToken: cancellationToken);
        }
    }
}