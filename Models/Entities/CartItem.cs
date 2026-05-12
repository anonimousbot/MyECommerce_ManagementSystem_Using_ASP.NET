using EMS.Contracts.Entities;

namespace EMS.Models.Entities
{
    public class CartItem : BaseEntity
    {
        public Guid CartId { get; set; }
        public Cart Cart { get; set; }

        public Guid ItemId { get; set; }
        public Item Item { get; set; }

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}

