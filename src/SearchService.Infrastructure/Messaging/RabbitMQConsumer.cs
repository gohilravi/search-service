using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SearchService.Core.Interfaces;
using SearchService.Core.Models;

namespace SearchService.Infrastructure.Messaging;

public class RabbitMQConsumer : BackgroundService
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly IEventProcessor _eventProcessor;
    private readonly ILogger<RabbitMQConsumer> _logger;
    private const string ExchangeName = "entity_events";

    public RabbitMQConsumer(
        IEventProcessor eventProcessor,
        ILogger<RabbitMQConsumer> logger,
        IConfiguration configuration)
    {
        _eventProcessor = eventProcessor;
        _logger = logger;

        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:HostName"] ?? "localhost",
            Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = configuration["RabbitMQ:UserName"] ?? "guest",
            Password = configuration["RabbitMQ:Password"] ?? "guest"
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Declare topic exchange
        _channel.ExchangeDeclare(ExchangeName, ExchangeType.Topic, durable: true);

        // Declare queues
        DeclareQueue("search.offer.events", "offer.*");
        DeclareQueue("search.purchase.events", "purchase.*");
        DeclareQueue("search.transport.events", "transport.*");
    }

    private void DeclareQueue(string queueName, string routingKey)
    {
        _channel.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(queueName, ExchangeName, routingKey);

        // Declare dead letter queue
        var dlqName = $"{queueName}.dlq";
        _channel.QueueDeclare(dlqName, durable: true, exclusive: false, autoDelete: false);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queues = new[] { "search.offer.events", "search.purchase.events", "search.transport.events" };

        foreach (var queueName in queues)
        {
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var routingKey = ea.RoutingKey;

                try
                {
                    var eventMessage = JsonSerializer.Deserialize<EventMessage>(message);
                    if (eventMessage != null)
                    {
                        await _eventProcessor.ProcessEventAsync(eventMessage);
                        _channel.BasicAck(ea.DeliveryTag, false);
                        _logger.LogInformation("Processed event: {EventType} for {EntityType}", 
                            eventMessage.EventType, eventMessage.EntityType);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to deserialize event message from queue {Queue}", queueName);
                        _channel.BasicNack(ea.DeliveryTag, false, false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message from queue {Queue}", queueName);
                    _channel.BasicNack(ea.DeliveryTag, false, false);
                }
            };

            _channel.BasicConsume(queueName, autoAck: false, consumer);
            _logger.LogInformation("Started consuming from queue: {Queue}", queueName);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}

