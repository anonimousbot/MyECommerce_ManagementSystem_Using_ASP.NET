namespace EMS.Models.DTOs.Payments
{
    public class PaymentVerificationResultDto
    {
        public bool Paid { get; set; }
        public string Reference { get; set; }
        public string Message { get; set; }
        public Guid? OrderId { get; set; }
    }
}

