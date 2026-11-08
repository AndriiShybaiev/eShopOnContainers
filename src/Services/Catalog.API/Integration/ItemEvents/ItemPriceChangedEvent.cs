namespace Catalog.API.Integration.ItemEvents;

public class ItemPriceChangedEvent
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
