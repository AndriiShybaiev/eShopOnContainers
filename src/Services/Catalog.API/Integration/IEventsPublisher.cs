namespace Catalog.API.Integration;

public interface IEventsPublisher
{
    Task Start(CancellationToken cancellationToken = default);
    Task Stop(CancellationToken cancellationToken = default);
    Task<bool> Publish(object message, CancellationToken cancellationToken = default);
}
