using EMS.Models.DTOs.Orders;
using FluentValidation;

namespace EMS.Models.DTOs.Orders.Validation
{
    public class CreateOrderValidation : AbstractValidator<CreateOrderRequestModel>
    {
        public CreateOrderValidation()
        {
            RuleFor(x => x.CustomerId).NotEmpty().WithMessage("CustomerId is required");
            RuleFor(x => x.Amount).NotEmpty().WithMessage("Amount is required");
            //RuleFor(x => x.TotalAmount).NotEmpty().WithMessage("Total Amount Cannot be Zero");
            RuleFor(x => x.DeliveryAddress).NotEmpty().WithMessage("Delivery Address is Required");
            RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity is required");
        }
    }
}
