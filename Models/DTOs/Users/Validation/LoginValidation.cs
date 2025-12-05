using EMS.Models.DTOs.Users;
using FluentValidation;

namespace EMS.Models.DTOs.Users.Validation
{
    public class LoginValidation :AbstractValidator<LoginRequestModel>
    {
        public LoginValidation()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Invalid email format.");

            RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.");
        }
    }
}
