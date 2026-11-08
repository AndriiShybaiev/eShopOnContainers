using System.ComponentModel.DataAnnotations;

namespace Catalog.API.Model;

public class CatalogItemDTO
{
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public decimal Price { get; set; }

    [Required]
    public string CatalogType { get; set; } = string.Empty;

    [Required]
    public string CatalogBrand { get; set; } = string.Empty;

    public int AvailableStock { get; set; }
    public int RestockThreshold { get; set; }
    public int MaxStockThreshold { get; set; }
    public bool OnReorder { get; set; }

    public CatalogItemDTO()
    {
    }

    public CatalogItemDTO(CatalogItem item)
    {
        Name = item.Name;
        Description = item.Description;
        Price = item.Price;
        CatalogType = item.CatalogType?.Type ?? string.Empty;
        CatalogBrand = item.CatalogBrand?.Brand ?? string.Empty;
        AvailableStock = item.AvailableStock;
        RestockThreshold = item.RestockThreshold;
        MaxStockThreshold = item.MaxStockThreshold;
        OnReorder = item.OnReorder;
    }
}
