using FluentValidation;

namespace EMS.Models.DTOs.Customers.Validation
{
    public class UpdateCustomerValidation : AbstractValidator<UpdateCustomerRequestModel>
    {
        public UpdateCustomerValidation() 
        {
            RuleFor(x => x.FirstName).Length(3, 50).NotEmpty().WithMessage("Firstname is required");
            RuleFor(x => x.LastName).Length(3, 50).NotEmpty().WithMessage("Lastname is required");
            RuleFor(x => x.Address).NotEmpty().WithMessage("Address is required");
            RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage("Phone number is required")
                .Matches("^[0-9]+$").WithMessage("Phone Number must contain only numbers")
                .Length(11).WithMessage("Phone number must be 11 Digits ");
            RuleFor(x => x.Gender).IsInEnum().NotEmpty().WithMessage("Gender is required");
        }
    }
}
