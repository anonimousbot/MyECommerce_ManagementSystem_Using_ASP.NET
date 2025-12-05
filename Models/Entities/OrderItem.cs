using EMS.Contracts.Entities;

namespace EMS.Models.Entities
{
    public class OrderItem : BaseEntity
    {
        public Guid OrderId { get; set; }
        public Order Order { get; set; }
        public Guid ItemId { get; set; }
        public Item Item { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice {  get; set; }
        public decimal TotalPrice { get; set; }
    }
}
