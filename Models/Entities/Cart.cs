using EMS.Contracts.Entities;

namespace EMS.Models.Entities
{
    public class Cart : BaseEntity
    {
        public Guid CustomerId { get; set; }
        public Customer Customer { get; set; }

        public ICollection<CartItem> Items { get; set; } = [];
    }
}

