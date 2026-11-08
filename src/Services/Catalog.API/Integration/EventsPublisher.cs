using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Catalog.API.Integration;

public class EventsPublisher : IEventsPublisher, IAsyncDisposable
{
    private readonly RabbitOptions _rabbitOptions;
    private readonly RabbitMqExchange _exchangeOptions;
    private readonly ILogger<EventsPublisher> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private IConnection? _connection;
    private IChannel? _channel;
    private bool _isStopped;

    public EventsPublisher(
        IOptions<RabbitOptions> rabbitOptions,
        IOptions<RabbitMqExchange> exchangeOptions,
        ILogger<EventsPublisher> logger)
    {
        _rabbitOptions = rabbitOptions?.Value ?? throw new ArgumentNullException(nameof(rabbitOptions));
        _exchangeOptions = exchangeOptions?.Value ?? throw new ArgumentNullException(nameof(exchangeOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Start(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _isStopped = false;
            await EnsureChannelAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to connect to RabbitMQ during startup on host {Host}:{Port}. Will retry on demand.", _rabbitOptions.HostName, _rabbitOptions.Port);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> Publish(object message, CancellationToken cancellationToken = default)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        if (_isStopped)
        {
            _logger.LogWarning("Cannot publish event because EventsPublisher is stopped.");
            return false;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await EnsureChannelAsync(cancellationToken);

            if (_channel == null || !_channel.IsOpen)
            {
                _logger.LogError("Unable to publish message because RabbitMQ channel is not open.");
                return false;
            }

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var props = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent
            };

            await _channel.BasicPublishAsync(
                exchange: _exchangeOptions.Exchange,
                routingKey: _exchangeOptions.RoutingKeys,
                mandatory: false,
                basicProperties: props,
                body: body,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Successfully published event to exchange {Exchange} with routing key {RoutingKey}: {Json}",
                _exchangeOptions.Exchange, _exchangeOptions.RoutingKeys, json);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while publishing event to exchange {Exchange}", _exchangeOptions.Exchange);
            return false;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task Stop(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _isStopped = true;
            await CloseConnectionAsync();
            _logger.LogInformation("EventsPublisher stopped successfully.");
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task EnsureChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel != null && _channel.IsOpen)
        {
            return;
        }

        await CloseConnectionAsync();

        var factory = new ConnectionFactory
        {
            HostName = _rabbitOptions.HostName,
            Port = _rabbitOptions.Port,
            UserName = _rabbitOptions.UserName,
            Password = _rabbitOptions.Password
        };

        const int maxRetries = 5;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("Connecting to RabbitMQ at {Host}:{Port} (attempt {Attempt}/{MaxRetries})...",
                    _rabbitOptions.HostName, _rabbitOptions.Port, attempt, maxRetries);

                _connection = await factory.CreateConnectionAsync(cancellationToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

                await _channel.ExchangeDeclareAsync(
                    exchange: _exchangeOptions.Exchange,
                    type: ExchangeType.Direct,
                    durable: true,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: cancellationToken);

                await _channel.QueueDeclareAsync(
                    queue: _exchangeOptions.QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: cancellationToken);

                await _channel.QueueBindAsync(
                    queue: _exchangeOptions.QueueName,
                    exchange: _exchangeOptions.Exchange,
                    routingKey: _exchangeOptions.RoutingKeys,
                    arguments: null,
                    cancellationToken: cancellationToken);

                _logger.LogInformation("RabbitMQ channel initialized. Exchange '{Exchange}', Queue '{Queue}', RoutingKey '{RoutingKey}'.",
                    _exchangeOptions.Exchange, _exchangeOptions.QueueName, _exchangeOptions.RoutingKeys);

                return;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex, "Failed to connect to RabbitMQ on attempt {Attempt}/{MaxRetries}. Retrying in 2s...", attempt, maxRetries);
                await Task.Delay(2000, cancellationToken);
            }
        }
    }

    private async Task CloseConnectionAsync()
    {
        try
        {
            if (_channel != null)
            {
                if (_channel.IsOpen)
                {
                    await _channel.CloseAsync();
                }
                await _channel.DisposeAsync();
                _channel = null;
            }

            if (_connection != null)
            {
                if (_connection.IsOpen)
                {
                    await _connection.CloseAsync();
                }
                await _connection.DisposeAsync();
                _connection = null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error while closing RabbitMQ channel/connection.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await Stop();
        _lock.Dispose();
        GC.SuppressFinalize(this);
    }
}
