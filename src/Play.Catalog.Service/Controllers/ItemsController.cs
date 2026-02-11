using MassTransit;
using MassTransit.Testing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Play.Catalog.Contracts;
using Play.Catalog.Service.DTOs;
using Play.Catalog.Service.Entities;
using Play.Common.Repositories;
using Play.Catalog.Service.Policies;

namespace Play.Catalog.Service.Controllers
{

    [ApiController]
    [Route("items")]
    public class ItemsController : ControllerBase
    {
        private readonly IRepository<Item> _itemsRepository;
        private readonly IPublishEndpoint _publishEndpoint;
        private const string AdminRole = "Admin";
        public ItemsController(IRepository<Item> itemsRepository, IPublishEndpoint publishEndpoint)
        {
            _itemsRepository = itemsRepository;
            _publishEndpoint = publishEndpoint;
        }

        // GET /items/{id}
        [HttpGet]
        [Authorize("CatalogReadOrAdmin")]
        public async Task<ActionResult<IEnumerable<ItemDto>>> GetAllAsync()
        {
            var items = (await _itemsRepository.GetAllAsync())
                           .Select(c => c.ToDto()).ToList();

            return Ok(items);
        }

        // GET /items/{id}
        [HttpGet("{id:guid}", Name = "GetByIdAsync")]
        [Authorize("CatalogReadOrAdmin")]
        public async Task<ActionResult<ItemDto>> GetByIdAsync(Guid id)
        {
            var item = await _itemsRepository.GetAsync(id);
            if (item is null)
            {
                return NotFound();
            }

            return item.ToDto();
        }

        // POST /items
        [HttpPost]
        [Authorize(Policies.Policies.Write)]
        public async Task<ActionResult<ItemDto>> PostAsync(CreateItemDto createItemDto)
        {
            var item = new Item
            {
                Id = Guid.NewGuid(),
                Name = createItemDto.Name,
                CreatedDate = DateTime.UtcNow,
                Description = createItemDto.Description,
                Price = createItemDto.Price
            };

            await _itemsRepository.CreateAsync(item);

            var created = await _itemsRepository.GetAsync(item.Id);
            if (created is null)
            {
                return NotFound();
            }

            await _publishEndpoint.Publish(new CatalogItemCreated(
                created.Id, 
                created.Name, 
                created.Description,
                created.Price
                ));

            return CreatedAtRoute(
            "GetByIdAsync",           
            new { id = created.Id },
            created);
        }

        // PUT /items/{id}
        [HttpPut("{id}")]
        [Authorize(Policies.Policies.Write)]
        public async Task<ActionResult> PutAsync(Guid id, UpdateItemDto updateItemDto)
        {
            var existingItem = await _itemsRepository.GetAsync(id);
            if (existingItem is null)
            {
                return NotFound();
            }

            existingItem.Name = updateItemDto.Name;
            existingItem.Description = updateItemDto.Description;
            existingItem.Price = updateItemDto.Price;

            await _itemsRepository.UpdateAsync(existingItem);

            await _publishEndpoint.Publish(new CatalogItemUpdated(
                existingItem.Id,
                existingItem.Name, 
                existingItem.Description,
                existingItem.Price));

            return NoContent();
        }

        //// DELETE /items/{id}
        [HttpDelete("{id}")]
        [Authorize(Policies.Policies.Write)]
        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            var item = await _itemsRepository.GetAsync(id);
            if (item is null)
            {
                return NotFound();
            }
            await _itemsRepository.RemoveAsync(item.Id);

            await _publishEndpoint.Publish(new CatalogItemDeleted(item.Id));

            return NoContent();
        }
    }

}
