using EMS.Models.Entities;

namespace EMS.Models.DTOs.Items
{
    public class ItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Brand { get; set; }
        public decimal Price { get; set; }
        public int QuantityInStock { get; set; }
        // COllection of OrderItems
        public ICollection<OrderItem>? OrderItem { get; set; } = []; 
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
    }
}
