using EMS.Models.DTOs.Customers;
using EMS.Models.Entities;
using EMS.Models.Enums;

namespace EMS.Models.DTOs.Orders
{
    public class OrderDto
    {
        public Guid Id { get; set; }
        public DateTime DateCreated { get; set; }
        public string ItemName { get; set; }
        public string DeliveryAddress { get; set; }
        public DateTime DateModified { get; set; }
        public Guid CustomerId { get; set; }
        public Customer? Customer { get; set; } 
        public decimal Amount { get; set; }
        public Status OrderStatus { get; set; }
        public ICollection<OrderItem>? OrderItem { get; set; } = [];
    }
}
