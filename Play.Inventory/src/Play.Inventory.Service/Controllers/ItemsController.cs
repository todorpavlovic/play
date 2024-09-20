using Microsoft.AspNetCore.Mvc;
using Play.Common;
using Play.Inventory.Service.Clients;
using Play.Inventory.Service.Dtos;
using Play.Inventory.Service.Entities;

namespace Play.Inventory.Service.Controllers;

[ApiController]
[Route("api/items")]
public class ItemsController : ControllerBase
{

    private readonly IRepository<CatalogItem> _catalogItemsRepository;
    private readonly IRepository<InventoryItem> _inventoryItemsRepository;
    private readonly CatalogClient _catalogClient;

    public ItemsController(IRepository<InventoryItem> inventoryItemsRepository, IRepository<CatalogItem> catalogItemsRepository,CatalogClient catalogClient)
    {
        _inventoryItemsRepository = inventoryItemsRepository;
        _catalogItemsRepository = catalogItemsRepository;
        _catalogClient = catalogClient;
    }


    [HttpGet("{userId}")]
    public async Task<ActionResult<IEnumerable<InventoryItemDto>>> Get(Guid userId)
    {
        if (userId == Guid.Empty) return BadRequest();

        var inventoryItemEntities = await _inventoryItemsRepository
                                    .GetAllAsync((item) => item.UserId == userId);
        var itemIds = inventoryItemEntities.Select(item => item.CatalogItemId);

        var catalogItemsEntities = await _catalogItemsRepository
                                    .GetAllAsync((item) => itemIds.Contains(item.Id));

        var inventoryItemDtos = inventoryItemEntities.Select(inventoryItem =>
        {
            var catalogItem = catalogItemsEntities?.Single(catalogItem =>
                                catalogItem.Id == inventoryItem.CatalogItemId);

            return inventoryItem.AsDto(catalogItem?.Name, catalogItem?.Description);
        });

        return Ok(inventoryItemDtos);
    }

    [HttpPost]
    public async Task<ActionResult> Post(GrantItemsDto grantItemsDto)
    {

        var inventoryItem = await _inventoryItemsRepository.GetAsync(item =>
        item.UserId == grantItemsDto.UserId &&
        item.CatalogItemId == grantItemsDto.CatalogItemId);

        if (inventoryItem is null)
        {
            inventoryItem = new InventoryItem
            {
                UserId = grantItemsDto.UserId,
                CatalogItemId = grantItemsDto.CatalogItemId,
                Quantity = grantItemsDto.Quantity,
                AcquiredDate = DateTimeOffset.UtcNow,
            };

            await _inventoryItemsRepository.CreateAsync(inventoryItem);
        }
        else
        {
            inventoryItem.Quantity += grantItemsDto.Quantity;
            await _inventoryItemsRepository.UpdateAsync(inventoryItem);
        }

        return Ok();
    }
}