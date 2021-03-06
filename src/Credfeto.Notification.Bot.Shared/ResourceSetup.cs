using Credfeto.Notification.Bot.Shared.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Credfeto.Notification.Bot.Shared;

public static class ResourceSetup
{
    public static IServiceCollection AddResources(this IServiceCollection services)
    {
        return services.AddSingleton<ICurrentTimeSource, CurrentTimeSource>()
                       .AddSingleton(typeof(IMessageChannel<>), typeof(MessageChannel<>))
                       .AddSingleton<IRunOnStartup, ProcessStartup>()
                       .AddHostedService<StartupService>();
    }
}