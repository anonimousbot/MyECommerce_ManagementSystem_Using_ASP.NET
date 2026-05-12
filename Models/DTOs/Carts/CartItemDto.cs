namespace EMS.Models.DTOs.Carts
{
    public class CartItemDto
    {
        public Guid ItemId { get; set; }
        public string Name { get; set; }
        public string Brand { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal Total => UnitPrice * Quantity;
    }
}

