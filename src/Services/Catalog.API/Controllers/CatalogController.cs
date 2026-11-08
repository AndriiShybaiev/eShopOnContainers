using System.Net;
using Catalog.API.Data;
using Catalog.API.Integration;
using Catalog.API.Integration.ItemEvents;
using Catalog.API.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Catalog.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CatalogController : ControllerBase
{
    private readonly CatalogContext _context;
    private readonly IEventsPublisher _eventsPublisher;
    private readonly ILogger<CatalogController> _logger;

    public CatalogController(
        CatalogContext context,
        IEventsPublisher eventsPublisher,
        ILogger<CatalogController> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _eventsPublisher = eventsPublisher ?? throw new ArgumentNullException(nameof(eventsPublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // GET api/Catalog?pageSize=10&pageIndex=0
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedItemsDTO<CatalogItemDTO>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<PaginatedItemsDTO<CatalogItemDTO>>> ItemsAsync(
        [FromQuery] int pageSize = 10,
        [FromQuery] int pageIndex = 0)
    {
        if (pageSize <= 0 || pageIndex < 0)
        {
            return BadRequest("pageSize must be greater than 0 and pageIndex must be non-negative.");
        }

        var totalItems = await _context.CatalogItems.LongCountAsync();

        var itemsOnPage = await _context.CatalogItems
            .Include(c => c.CatalogBrand)
            .Include(c => c.CatalogType)
            .OrderBy(c => c.Name)
            .Skip(pageSize * pageIndex)
            .Take(pageSize)
            .ToListAsync();

        var itemsDto = itemsOnPage.Select(item => new CatalogItemDTO(item));

        return Ok(new PaginatedItemsDTO<CatalogItemDTO>(pageIndex, pageSize, totalItems, itemsDto));
    }

    // GET api/Catalog/{id}
    [HttpGet("{id:int}")]
    [ActionName(nameof(GetItemByIdAsync))]
    [ProducesResponseType(typeof(CatalogItemDTO), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<CatalogItemDTO>> GetItemByIdAsync(int id)
    {
        if (id <= 0)
        {
            return BadRequest();
        }

        var item = await _context.CatalogItems
            .Include(ci => ci.CatalogBrand)
            .Include(ci => ci.CatalogType)
            .SingleOrDefaultAsync(ci => ci.Id == id);

        if (item == null)
        {
            return NotFound();
        }

        return Ok(new CatalogItemDTO(item));
    }

    // POST api/Catalog
    [HttpPost]
    [ProducesResponseType(typeof(CatalogItemDTO), (int)HttpStatusCode.Created)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<CatalogItemDTO>> CreateProductAsync([FromBody] CatalogItemDTO product)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var brand = await _context.CatalogBrands.FirstOrDefaultAsync(b => b.Brand == product.CatalogBrand);
        var type = await _context.CatalogTypes.FirstOrDefaultAsync(t => t.Type == product.CatalogType);

        if (brand == null || type == null)
        {
            return BadRequest("Invalid CatalogBrand or CatalogType. Make sure the specified brand and type exist.");
        }

        var item = new CatalogItem
        {
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            CatalogBrandId = brand.Id,
            CatalogBrand = brand,
            CatalogTypeId = type.Id,
            CatalogType = type,
            AvailableStock = product.AvailableStock,
            RestockThreshold = product.RestockThreshold,
            MaxStockThreshold = product.MaxStockThreshold,
            OnReorder = product.OnReorder
        };

        _context.CatalogItems.Add(item);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetItemByIdAsync), new { id = item.Id }, new CatalogItemDTO(item));
    }

    // PUT api/Catalog/{id}
    [HttpPut("{id:int}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult> UpdateProductAsync(int id, [FromBody] CatalogItemDTO productToUpdate)
    {
        if (id <= 0)
        {
            return BadRequest("Invalid id.");
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var catalogItem = await _context.CatalogItems
            .Include(ci => ci.CatalogBrand)
            .Include(ci => ci.CatalogType)
            .SingleOrDefaultAsync(i => i.Id == id);

        if (catalogItem == null)
        {
            return NotFound(new { Message = $"Item with id {id} not found." });
        }

        var brand = await _context.CatalogBrands.FirstOrDefaultAsync(b => b.Brand == productToUpdate.CatalogBrand);
        var type = await _context.CatalogTypes.FirstOrDefaultAsync(t => t.Type == productToUpdate.CatalogType);

        if (brand == null || type == null)
        {
            return BadRequest("Invalid CatalogBrand or CatalogType. Make sure the specified brand and type exist.");
        }

        var oldPrice = catalogItem.Price;
        var priceChanged = oldPrice != productToUpdate.Price;

        catalogItem.Name = productToUpdate.Name;
        catalogItem.Description = productToUpdate.Description;
        catalogItem.Price = productToUpdate.Price;
        catalogItem.CatalogBrandId = brand.Id;
        catalogItem.CatalogBrand = brand;
        catalogItem.CatalogTypeId = type.Id;
        catalogItem.CatalogType = type;
        catalogItem.AvailableStock = productToUpdate.AvailableStock;
        catalogItem.RestockThreshold = productToUpdate.RestockThreshold;
        catalogItem.MaxStockThreshold = productToUpdate.MaxStockThreshold;
        catalogItem.OnReorder = productToUpdate.OnReorder;

        _context.CatalogItems.Update(catalogItem);
        await _context.SaveChangesAsync();

        if (priceChanged)
        {
            var priceChangedEvent = new ItemPriceChangedEvent
            {
                Name = catalogItem.Name,
                Price = catalogItem.Price
            };

            var published = await _eventsPublisher.Publish(priceChangedEvent);
            if (!published)
            {
                _logger.LogWarning("Failed to publish ItemPriceChangedEvent for item {ItemName} (Id: {ItemId})", catalogItem.Name, catalogItem.Id);
            }
        }

        return NoContent();
    }

    // DELETE api/Catalog/{id}
    [HttpDelete("{id:int}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public async Task<ActionResult> DeleteProductAsync(int id)
    {
        if (id <= 0)
        {
            return BadRequest();
        }

        var item = await _context.CatalogItems.SingleOrDefaultAsync(p => p.Id == id);

        if (item == null)
        {
            return NotFound();
        }

        _context.CatalogItems.Remove(item);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
