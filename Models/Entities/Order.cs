using EMS.Contracts.Entities;
using EMS.Models.Enums;

namespace EMS.Models.Entities
{
    public class Order : BaseEntity
    {
        public Guid CustomerId {  get; set; }
        public Customer Customer { get; set; }
        public  string DeliveryAddress {  get; set; }
        public decimal Amount { get; set; }
        public Status OrderStatus { get; set; }
        public string? PaymentReference { get; set; }

        // Colection Of Order Item
        public ICollection<OrderItem>? OrderItem { get; set; } = [];
    }
}
