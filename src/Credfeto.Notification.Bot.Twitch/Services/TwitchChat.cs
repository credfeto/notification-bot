using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Credfeto.Notification.Bot.Twitch.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Models;
using ChannelState = Credfeto.Notification.Bot.Twitch.Models.ChannelState;

namespace Credfeto.Notification.Bot.Twitch.Services;

public sealed class TwitchChat : ITwitchChat
{
    private readonly TwitchClient _client;
    private readonly ILogger<TwitchChat> _logger;

    private readonly TwitchBotOptions _options;
    private readonly ITwitchChannelManager _twitchChannelManager;
    private bool _connected;

    public TwitchChat(IOptions<TwitchBotOptions> options, ITwitchChannelManager twitchChannelManager, ILogger<TwitchChat> logger)
    {
        this._twitchChannelManager = twitchChannelManager ?? throw new ArgumentNullException(nameof(twitchChannelManager));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this._options = (options ?? throw new ArgumentNullException(nameof(options))).Value;

        List<string> channels = new[]
                                {
                                    this._options.Authentication.UserName
                                }.Concat(this._options.Channels)
                                 .Concat(this._options.Heists)
                                 .Select(c => c.ToLowerInvariant())
                                 .Distinct()
                                 .ToList();

        ConnectionCredentials credentials = new(twitchUsername: this._options.Authentication.UserName, twitchOAuth: this._options.Authentication.OAuthToken);
        TwitchClient client = new(new WebSocketClient(new ClientOptions
                                                      {
                                                          MessagesAllowedInPeriod = 750,
                                                          ThrottlingPeriod = TimeSpan.FromSeconds(30),
                                                          ReconnectionPolicy = new(reconnectInterval: 1000, maxAttempts: null)
                                                      })) { OverrideBeingHostedCheck = true, AutoReListenOnException = false };

        client.Initialize(credentials: credentials, channels: channels);
        this._client = client;

        // HEALTH
        Observable.FromEventPattern<OnConnectedArgs>(addHandler: h => this._client.OnConnected += h, removeHandler: h => this._client.OnConnected -= h)
                  .Select(messageEvent => messageEvent.EventArgs)
                  .Subscribe(this.Client_OnConnected);

        Observable.FromEventPattern<OnDisconnectedEventArgs>(addHandler: h => this._client.OnDisconnected += h, removeHandler: h => this._client.OnDisconnected -= h)
                  .Select(messageEvent => messageEvent.EventArgs)
                  .Subscribe(this.Client_OnDisconnected);

        Observable.FromEventPattern<OnReconnectedEventArgs>(addHandler: h => this._client.OnReconnected += h, removeHandler: h => this._client.OnReconnected -= h)
                  .Select(messageEvent => messageEvent.EventArgs)
                  .Subscribe(this.Client_OnReconnected);

        Observable.FromEventPattern<OnJoinedChannelArgs>(addHandler: h => this._client.OnJoinedChannel += h, removeHandler: h => this._client.OnJoinedChannel -= h)
                  .Select(messageEvent => messageEvent.EventArgs)
                  .Subscribe(this.Client_OnJoinedChannel);

        // STATE
        Observable.FromEventPattern<OnChannelStateChangedArgs>(addHandler: h => this._client.OnChannelStateChanged += h, removeHandler: h => this._client.OnChannelStateChanged -= h)
                  .Select(messageEvent => messageEvent.EventArgs)
                  .Subscribe(this.Client_OnChannelStateChanged);

        // LOGGING
        Observable.FromEventPattern<OnLogArgs>(addHandler: h => this._client.OnLog += h, removeHandler: h => this._client.OnLog -= h)
                  .Select(messageEvent => messageEvent.EventArgs)
                  .Subscribe(this.Client_OnLog);

        // CHAT
        Observable.FromEventPattern<OnMessageReceivedArgs>(addHandler: h => this._client.OnMessageReceived += h, removeHandler: h => this._client.OnMessageReceived -= h)
                  .Select(messageEvent => messageEvent.EventArgs)
                  .Subscribe(this.Client_OnMessageReceived);

        Observable.FromEventPattern<OnChatClearedArgs>(addHandler: h => this._client.OnChatCleared += h, removeHandler: h => this._client.OnChatCleared -= h)
                  .Select(messageEvent => messageEvent.EventArgs)
                  .Subscribe(this.Client_OnChatCleared);

        // RAID HOST
        Observable.FromEventPattern<OnRaidNotificationArgs>(addHandler: h => this._client.OnRaidNotification += h, removeHandler: h => this._client.OnRaidNotification -= h)
                  .Select(messageEvent => messageEvent.EventArgs)
                  .Subscribe(this.Client_OnRaided);

        Observable.FromEventPattern<OnBeingHostedArgs>(addHandler: h => this._client.OnBeingHosted += h, removeHandler: h => this._client.OnBeingHosted -= h)
                  .Select(messageEvent => messageEvent.EventArgs)
                  .Subscribe(this.Client_OnBeingHosted);

        // SUBS
        Observable.FromEventPattern<OnNewSubscriberArgs>(addHandler: h => this._client.OnNewSubscriber += h, removeHandler: h => this._client.OnNewSubscriber -= h)
                  .Select(messageEvent => messageEvent.EventArgs)
                  .Subscribe(this.Client_OnNewSubscriber);

        Observable.FromEventPattern<OnReSubscriberArgs>(addHandler: h => this._client.OnReSubscriber += h, removeHandler: h => this._client.OnReSubscriber -= h)
                  .Select(messageEvent => messageEvent.EventArgs)
                  .Subscribe(this.Client_OnReSubscriber);

        Observable.FromEventPattern<OnCommunitySubscriptionArgs>(addHandler: h => this._client.OnCommunitySubscription += h, removeHandler: h => this._client.OnCommunitySubscription -= h)
                  .Select(messageEvent => messageEvent.EventArgs)
                  .Subscribe(this.Client_OnCommunitySubscription);

        Observable.FromEventPattern<OnGiftedSubscriptionArgs>(addHandler: h => this._client.OnGiftedSubscription += h, removeHandler: h => this._client.OnGiftedSubscription -= h)
                  .Select(messageEvent => messageEvent.EventArgs)
                  .Subscribe(this.Client_OnGiftedSubscription);

        Observable.FromEventPattern<OnContinuedGiftedSubscriptionArgs>(addHandler: h => this._client.OnContinuedGiftedSubscription += h,
                                                                       removeHandler: h => this._client.OnContinuedGiftedSubscription -= h)
                  .Select(messageEvent => messageEvent.EventArgs)
                  .Subscribe(this.Client_OnContinuedGiftedSubscription);

        Observable.FromEventPattern<OnPrimePaidSubscriberArgs>(addHandler: h => this._client.OnPrimePaidSubscriber += h, removeHandler: h => this._client.OnPrimePaidSubscriber -= h)
                  .Select(messageEvent => messageEvent.EventArgs)
                  .Subscribe(this.Client_OnPrimePaidSubscriber);

        this._client.Connect();
        this._connected = true;
    }

    public Task UpdateAsync()
    {
        if (!this._connected)
        {
            this._logger.LogDebug("Reconnecting...");
            this._client.Connect();
            this._connected = true;
        }

        return Task.CompletedTask;

        //return Task.CompletedTask;
    }

    private void Client_OnBeingHosted(OnBeingHostedArgs e)
    {
        if (!this._options.Heists.Any(c => StringComparer.InvariantCultureIgnoreCase.Equals(x: c, y: e.BeingHostedNotification.Channel)))
        {
            return;
        }

        this._logger.LogInformation(
            $"{e.BeingHostedNotification.Channel}: Being hosted by {e.BeingHostedNotification.HostedByChannel} Viewers: {e.BeingHostedNotification.Viewers}, AutoHost: {e.BeingHostedNotification.IsAutoHosted}");
    }

    private void Client_OnChatCleared(OnChatClearedArgs e)
    {
        if (!this.IsModChannel(e.Channel))
        {
            return;
        }

        this._logger.LogInformation($"{e.Channel}: Chat Cleared");

        ChannelState state = this._twitchChannelManager.GetChannel(e.Channel);

        state.ClearChat();
    }

    private void Client_OnCommunitySubscription(OnCommunitySubscriptionArgs e)
    {
        if (!this.IsModChannel(e.Channel))
        {
            return;
        }

        this._logger.LogInformation($"{e.Channel}: Community Sub: {e.GiftedSubscription.DisplayName}");

        if (e.GiftedSubscription.IsAnonymous)
        {
            return;
        }

        ChannelState state = this._twitchChannelManager.GetChannel(e.Channel);

        state.GiftedMultiple(giftedBy: e.GiftedSubscription.DisplayName, count: e.GiftedSubscription.MsgParamMassGiftCount, months: e.GiftedSubscription.MsgParamMultiMonthGiftDuration);
    }

    private void Client_OnGiftedSubscription(OnGiftedSubscriptionArgs e)
    {
        if (!this.IsModChannel(e.Channel))
        {
            return;
        }

        this._logger.LogInformation($"{e.Channel}: Community Sub: {e.GiftedSubscription.DisplayName}");

        if (e.GiftedSubscription.IsAnonymous)
        {
            return;
        }

        ChannelState state = this._twitchChannelManager.GetChannel(e.Channel);

        state.GiftedSub(giftedBy: e.GiftedSubscription.DisplayName, months: e.GiftedSubscription.MsgParamMultiMonthGiftDuration);
    }

    private bool IsModChannel(string channel)
    {
        return this._options.Channels.Any(c => StringComparer.InvariantCultureIgnoreCase.Equals(x: c, y: channel));
    }

    private void Client_OnChannelStateChanged(OnChannelStateChangedArgs e)
    {
        if (!this.IsModChannel(e.Channel))
        {
            return;
        }

        this._logger.LogInformation($"{e.Channel}: Emote Only: {e.ChannelState.EmoteOnly} Follower Only: {e.ChannelState.FollowersOnly} Sub Only: {e.ChannelState.SubOnly}");
    }

    private void Client_OnContinuedGiftedSubscription(OnContinuedGiftedSubscriptionArgs e)
    {
        if (!this.IsModChannel(e.Channel))
        {
            return;
        }

        this._logger.LogInformation($"{e.Channel}: {e.ContinuedGiftedSubscription.DisplayName} continued sub gifted by {e.ContinuedGiftedSubscription.MsgParamSenderLogin}");

        ChannelState state = this._twitchChannelManager.GetChannel(e.Channel);

        state.ContinuedSub(e.ContinuedGiftedSubscription.DisplayName);
    }

    private void Client_OnPrimePaidSubscriber(OnPrimePaidSubscriberArgs e)
    {
        if (!this.IsModChannel(e.Channel))
        {
            return;
        }

        this._logger.LogInformation($"{e.Channel}: {e.PrimePaidSubscriber.DisplayName} converted prime sub to paid");

        ChannelState state = this._twitchChannelManager.GetChannel(e.Channel);

        state.PrimeToPaid(e.PrimePaidSubscriber.DisplayName);
    }

    private void Client_OnLog(OnLogArgs e)
    {
        this._logger.LogDebug($"{e.DateTime}: {e.BotUsername} - {e.Data}");
    }

    private void Client_OnConnected(OnConnectedArgs e)
    {
        this._logger.LogWarning($"Connected to {e.BotUsername} {e.AutoJoinChannel}");
        this._connected = true;
    }

    private void Client_OnDisconnected(OnDisconnectedEventArgs e)
    {
        this._logger.LogWarning("Disconnected :(");
        this._connected = false;
    }

    private void Client_OnReconnected(OnReconnectedEventArgs e)
    {
        this._logger.LogWarning("Reconnected :)");
        this._connected = true;
    }

    private void Client_OnRaided(OnRaidNotificationArgs e)
    {
        if (!this.IsModChannel(e.Channel))
        {
            return;
        }

        this._logger.LogInformation($"Raided by {e.RaidNotification.DisplayName}");

        if (this._options.Raids.Contains(e.Channel))
        {
            this.IssueRaidWelcome(channel: e.Channel, raider: e.RaidNotification.DisplayName);
        }

        ChannelState state = this._twitchChannelManager.GetChannel(e.Channel);

        state.Raided(e.RaidNotification.DisplayName);
    }

    private void IssueRaidWelcome(string channel, string raider)
    {
        const string raidWelcome = @"
♫♫♫♫♫♫♫♫♫♫♫♫♫♫♫♫♫♫♫♫♫♫♫♫♫♫♫♫♫♫
GlitchLit  GlitchLit  GlitchLit Welcome raiders! GlitchLit GlitchLit GlitchLit
♫♫♫♫♫♫♫♫♫♫♫♫♫♫♫♫♫♫♫♫♫♫♫♫♫♫♫♫♫♫";

        this._client.SendMessage(channel: channel, message: raidWelcome);
        this._client.SendMessage(channel: channel, $"Thanks @{raider} for the raid");
        this._client.SendMessage(channel: channel, $"Check out https://www.twitch.tv/{raider}");
    }

    private void Client_OnJoinedChannel(OnJoinedChannelArgs e)
    {
        this._logger.LogInformation($"{e.Channel} Joining channel as {e.BotUsername}");
    }

    private void Client_OnMessageReceived(OnMessageReceivedArgs e)
    {
        this._logger.LogInformation($"{e.ChatMessage.Channel}: @{e.ChatMessage.Username}: {e.ChatMessage.Message}");

        if (e.ChatMessage.IsBroadcaster || e.ChatMessage.IsModerator)
        {
            return;
        }

        if (this._options.Heists.Contains(e.ChatMessage.Channel))
        {
            this.JoinHeist(e);
        }

        if (!this.IsModChannel(e.ChatMessage.Channel))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(e.ChatMessage.BotUsername))
        {
            return;
        }

        ChannelState state = this._twitchChannelManager.GetChannel(e.ChatMessage.Channel);

        if (state.ChatMessage(user: e.ChatMessage.Username, message: e.ChatMessage.Message, bits: e.ChatMessage.Bits))
        {
            // first time chatted in channel
            this.IssueShoutOut(channel: e.ChatMessage.Channel, user: e.ChatMessage.Username);
        }
    }

    private void JoinHeist(OnMessageReceivedArgs e)
    {
        //:streamlabs!streamlabs@streamlabs.tmi.twitch.tv PRIVMSG #emilyisfun :Ahoy! Captain reckless_fury is trying to get a crew together for a treasure hunt! Type !heist <amount> to join.
        if (StringComparer.InvariantCulture.Equals(x: e.ChatMessage.Username, y: "streamlabs") && e.ChatMessage.Message.StartsWith(value: "Ahoy! Captain ", comparisonType: StringComparison.Ordinal) &&
            e.ChatMessage.Message.EndsWith(value: " is trying to get a crew together for a treasure hunt! Type !heist <amount> to join.", comparisonType: StringComparison.Ordinal))
        {
            this._logger.LogInformation($"{e.ChatMessage.Channel}: Heist Starting!");

            //this._client.SendMessage(channel: e.ChatMessage.Channel, message: "!heist all");
        }
    }

    private void IssueShoutOut(string channel, string user)
    {
        this._logger.LogInformation($"{channel}: Checking if need to shoutout {user}");
        TwitchChannelShoutout? soChannel = this._options.Shoutouts.Find(c => StringComparer.InvariantCultureIgnoreCase.Equals(x: c.Channel, y: channel));

        if (soChannel == null)
        {
            return;
        }

        TwitchFriendChannel? streamer = soChannel.FriendChannels.Find(c => StringComparer.InvariantCultureIgnoreCase.Equals(x: c.Channel, y: user));

        if (streamer == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(streamer.Message))
        {
            this._client.SendMessage(channel: streamer.Channel, $"Check out https://www.twitch.tv/{user}");
        }
        else
        {
            this._client.SendMessage(channel: streamer.Channel, message: streamer.Message);
        }
    }

    private void Client_OnNewSubscriber(OnNewSubscriberArgs e)
    {
        if (!this.IsModChannel(e.Channel))
        {
            return;
        }

        this._logger.LogInformation($"{e.Channel}: New Subscriber {e.Subscriber.DisplayName}");

        ChannelState state = this._twitchChannelManager.GetChannel(e.Channel);

        if (e.Subscriber.SubscriptionPlan == SubscriptionPlan.Prime)
        {
            state.NewSubscriberPaid(e.Subscriber.DisplayName);
        }
        else
        {
            state.NewSubscriberPrime(e.Subscriber.DisplayName);
        }
    }

    private void Client_OnReSubscriber(OnReSubscriberArgs e)
    {
        if (!this.IsModChannel(e.Channel))
        {
            return;
        }

        this._logger.LogInformation($"{e.Channel}: Resub {e.ReSubscriber.DisplayName} for {e.ReSubscriber.Months}");

        ChannelState state = this._twitchChannelManager.GetChannel(e.Channel);

        if (e.ReSubscriber.SubscriptionPlan == SubscriptionPlan.Prime)
        {
            state.ResubscribePaid(user: e.ReSubscriber.DisplayName, months: e.ReSubscriber.Months);
        }
        else
        {
            state.ResubscribePrime(user: e.ReSubscriber.DisplayName, months: e.ReSubscriber.Months);
        }
    }
}