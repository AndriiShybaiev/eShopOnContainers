using Catalog.API.Model;
using Microsoft.EntityFrameworkCore;

namespace Catalog.API.Data;

public class CatalogContextSeed
{
    public async Task SeedAsync(CatalogContext context, ILogger<CatalogContextSeed> logger)
    {
        try
        {
            if (!await context.CatalogBrands.AnyAsync())
            {
                var brands = new List<CatalogBrand>
                {
                    new() { Brand = "Trek" },
                    new() { Brand = "Giant" },
                    new() { Brand = "Specialized" },
                    new() { Brand = "Xiaomi" },
                    new() { Brand = "Segway-Ninebot" },
                    new() { Brand = "Kugoo" }
                };
                await context.CatalogBrands.AddRangeAsync(brands);
                await context.SaveChangesAsync();
                logger.LogInformation("CatalogBrands seeded successfully.");
            }

            if (!await context.CatalogTypes.AnyAsync())
            {
                var types = new List<CatalogType>
                {
                    new() { Type = "Mountain Bike" },
                    new() { Type = "City Bike" },
                    new() { Type = "Electric Bike" },
                    new() { Type = "Electric Scooter" },
                    new() { Type = "Freestyle Scooter" },
                    new() { Type = "Kids Scooter" }
                };
                await context.CatalogTypes.AddRangeAsync(types);
                await context.SaveChangesAsync();
                logger.LogInformation("CatalogTypes seeded successfully.");
            }

            if (!await context.CatalogItems.AnyAsync())
            {
                var trek = await context.CatalogBrands.FirstAsync(b => b.Brand == "Trek");
                var giant = await context.CatalogBrands.FirstAsync(b => b.Brand == "Giant");
                var specialized = await context.CatalogBrands.FirstAsync(b => b.Brand == "Specialized");
                var xiaomi = await context.CatalogBrands.FirstAsync(b => b.Brand == "Xiaomi");
                var segway = await context.CatalogBrands.FirstAsync(b => b.Brand == "Segway-Ninebot");
                var kugoo = await context.CatalogBrands.FirstAsync(b => b.Brand == "Kugoo");

                var mountainBike = await context.CatalogTypes.FirstAsync(t => t.Type == "Mountain Bike");
                var cityBike = await context.CatalogTypes.FirstAsync(t => t.Type == "City Bike");
                var electricBike = await context.CatalogTypes.FirstAsync(t => t.Type == "Electric Bike");
                var electricScooter = await context.CatalogTypes.FirstAsync(t => t.Type == "Electric Scooter");
                var kidsScooter = await context.CatalogTypes.FirstAsync(t => t.Type == "Kids Scooter");

                var items = new List<CatalogItem>
                {
                    new()
                    {
                        Name = "Trek Marlin 7",
                        Description = "Cross-country mountain bike with front suspension",
                        Price = 850.00m,
                        CatalogBrandId = trek.Id,
                        CatalogTypeId = mountainBike.Id,
                        AvailableStock = 15,
                        RestockThreshold = 5,
                        MaxStockThreshold = 30,
                        OnReorder = false
                    },
                    new()
                    {
                        Name = "Giant Escape 3",
                        Description = "Comfortable city and fitness commuter bicycle",
                        Price = 480.00m,
                        CatalogBrandId = giant.Id,
                        CatalogTypeId = cityBike.Id,
                        AvailableStock = 20,
                        RestockThreshold = 5,
                        MaxStockThreshold = 40,
                        OnReorder = false
                    },
                    new()
                    {
                        Name = "Specialized Turbo Vado",
                        Description = "Premium electric city bike with integrated battery",
                        Price = 950.00m,
                        CatalogBrandId = specialized.Id,
                        CatalogTypeId = electricBike.Id,
                        AvailableStock = 8,
                        RestockThreshold = 2,
                        MaxStockThreshold = 15,
                        OnReorder = false
                    },
                    new()
                    {
                        Name = "Xiaomi Electric Scooter 4 Pro",
                        Description = "High performance electric scooter with 55km range",
                        Price = 599.99m,
                        CatalogBrandId = xiaomi.Id,
                        CatalogTypeId = electricScooter.Id,
                        AvailableStock = 25,
                        RestockThreshold = 5,
                        MaxStockThreshold = 50,
                        OnReorder = false
                    },
                    new()
                    {
                        Name = "Segway Ninebot Max G2",
                        Description = "Long-range e-scooter with hydraulic front suspension",
                        Price = 799.00m,
                        CatalogBrandId = segway.Id,
                        CatalogTypeId = electricScooter.Id,
                        AvailableStock = 18,
                        RestockThreshold = 4,
                        MaxStockThreshold = 35,
                        OnReorder = false
                    },
                    new()
                    {
                        Name = "Kugoo Kirin M4 Pro",
                        Description = "Off-road capable electric scooter with long battery life",
                        Price = 520.00m,
                        CatalogBrandId = kugoo.Id,
                        CatalogTypeId = electricScooter.Id,
                        AvailableStock = 12,
                        RestockThreshold = 3,
                        MaxStockThreshold = 25,
                        OnReorder = false
                    },
                    new()
                    {
                        Name = "Specialized Rockhopper",
                        Description = "Reliable trail hardtail mountain bike",
                        Price = 650.00m,
                        CatalogBrandId = specialized.Id,
                        CatalogTypeId = mountainBike.Id,
                        AvailableStock = 10,
                        RestockThreshold = 3,
                        MaxStockThreshold = 20,
                        OnReorder = false
                    },
                    new()
                    {
                        Name = "Xiaomi Mi Kids Scooter",
                        Description = "Safe three-wheel kick scooter for toddlers and kids",
                        Price = 65.50m,
                        CatalogBrandId = xiaomi.Id,
                        CatalogTypeId = kidsScooter.Id,
                        AvailableStock = 30,
                        RestockThreshold = 10,
                        MaxStockThreshold = 60,
                        OnReorder = false
                    }
                };

                await context.CatalogItems.AddRangeAsync(items);
                await context.SaveChangesAsync();
                logger.LogInformation("CatalogItems seeded successfully.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }
}
