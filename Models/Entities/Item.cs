using EMS.Contracts.Entities;

namespace EMS.Models.Entities
{
    public class Item : BaseEntity
    {
        public string Name {  get; set; }
        public string Brand { get; set; }
        public decimal Price { get; set; }
        public int QuantityInStock { get; set; }
        // COllection of OrderItems
        public ICollection<OrderItem>? OrderItem { get; set; } = [];
    }
}
