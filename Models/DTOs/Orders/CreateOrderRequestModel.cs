using EMS.Models.Enums;

namespace EMS.Models.DTOs.Orders
{
    public class CreateOrderRequestModel
    {
        public Guid ItemId { get; set; }
        public int Quantity { get; set; }
        public string DeliveryAddress { get; set; }
        public decimal Amount { get; set; }
        public Guid CustomerId { get; set; }
        public decimal TotalAmount => Quantity * Amount;
    }
}
