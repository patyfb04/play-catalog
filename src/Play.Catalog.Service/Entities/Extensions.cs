using Play.Catalog.Service.DTOs;

namespace Play.Catalog.Service.Entities
{
    public static class Extensions
    {
        public static ItemDto ToDto(this Item item)
        {
            return new ItemDto(item.Id, item.Name, item.Description, item.Price, item.CreatedDate);
        }
    }
}
