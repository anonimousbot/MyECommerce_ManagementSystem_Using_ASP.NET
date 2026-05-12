namespace EMS.Models.DTOs.Payments
{
    public class CheckoutSnapshotDto
    {
        public string DeliveryAddress { get; set; }
        public IReadOnlyList<CheckoutSnapshotItemDto> Items { get; set; } = [];
    }

    public class CheckoutSnapshotItemDto
    {
        public Guid ItemId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}

