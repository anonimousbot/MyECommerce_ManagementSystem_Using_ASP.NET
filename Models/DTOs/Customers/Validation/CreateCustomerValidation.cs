using FluentValidation;
namespace EMS.Models.DTOs.Customers.Validation
{
    public class CreateCustomerValidation : AbstractValidator<CreateCustomerRequestModel>
    {
        public CreateCustomerValidation()
        {
            RuleFor(x => x.FirstName).Length(3, 50).NotEmpty().WithMessage("Firstname is required");
            RuleFor(x => x.LastName).Length(3, 50).NotEmpty().WithMessage("Lastname is required");
            RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required").EmailAddress().WithMessage("Email Must Be In A Correct Format");
            RuleFor(x => x.PasswordHash).NotEmpty().WithMessage("Password is required");
            RuleFor(x => x.ConfirmPassword).NotEmpty().WithMessage("Password Must Match").Equal(x => x.PasswordHash).WithMessage("Password Must Match");
            RuleFor(x => x.Address).NotEmpty().WithMessage("Address is required");
            RuleFor(x => x.DateOfBirth).NotEmpty().WithMessage("Date of birth is required").LessThan(new DateTime(2016,1,1))
                .WithMessage("Date Of Birth Cannot be today or a future day").GreaterThan(new DateTime(1900,1,1));
            RuleFor(x => x.Gender).NotEmpty().WithMessage("Gender is required");
            RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage("Phone number is required")
                .Matches("^[0-9]+$").WithMessage("Phone Number must contain only numbers")
                .Length(11).WithMessage("Phone number must be 11 Digits ");
            
        }
    }
}
