using EMS.Contracts.Entities;
using EMS.Models.Enums;

namespace EMS.Models.Entities
{
    public class Payment : BaseEntity
    {
        public Guid? OrderId { get; set; }
        public Order? Order { get; set; }

        public Guid CustomerId { get; set; }
        public Customer Customer { get; set; }

        public decimal Amount { get; set; }
        public int AmountKobo { get; set; }
        public string Currency { get; set; } = "NGN";
        public string? Email { get; set; }

        public string Reference { get; set; } = string.Empty;
        public string CheckoutSnapshotJson { get; set; } = string.Empty;
        public PaymentStatus Status { get; set; }
        public DateTime? PaidAt { get; set; }
    }
}
