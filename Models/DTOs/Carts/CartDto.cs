namespace EMS.Models.DTOs.Carts
{
    public class CartDto
    {
        public Guid CustomerId { get; set; }
        public IReadOnlyList<CartItemDto> Items { get; set; } = [];
        public decimal Subtotal => Items.Sum(i => i.Total);
    }
}

