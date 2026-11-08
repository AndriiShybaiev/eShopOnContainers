namespace Catalog.API.Integration;

public class RabbitMqExchange
{
    public string Exchange { get; set; } = "catalogitems";
    public string QueueName { get; set; } = "itemspricechanged";
    public string RoutingKeys { get; set; } = "myRoutingKey";
}
