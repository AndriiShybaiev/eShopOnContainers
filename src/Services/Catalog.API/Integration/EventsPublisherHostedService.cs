using Microsoft.Extensions.Hosting;

namespace Catalog.API.Integration;

public class EventsPublisherHostedService : IHostedService
{
    private readonly IEventsPublisher _eventsPublisher;
    private readonly ILogger<EventsPublisherHostedService> _logger;

    public EventsPublisherHostedService(
        IEventsPublisher eventsPublisher,
        ILogger<EventsPublisherHostedService> logger)
    {
        _eventsPublisher = eventsPublisher ?? throw new ArgumentNullException(nameof(eventsPublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting EventsPublisherHostedService...");
        await _eventsPublisher.Start(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping EventsPublisherHostedService...");
        await _eventsPublisher.Stop(cancellationToken);
    }
}
