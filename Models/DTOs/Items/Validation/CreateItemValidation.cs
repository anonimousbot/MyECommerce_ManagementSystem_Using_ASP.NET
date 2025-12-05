using EMS.Models.DTOs.Items;
using FluentValidation;

namespace EMS.Models.DTOs.Items.Validation
{
    public class CreateItemValidation : AbstractValidator<CreateItemRequestModel>
    {
        public CreateItemValidation()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
            RuleFor(x => x.Brand).NotEmpty().WithMessage("BrandName is required");
            RuleFor(x => x.Price).NotEmpty().WithMessage("Price is required");
            RuleFor(x => x.QuantityInStock).NotEmpty().WithMessage("Quantity is Required");

        }
    }
}
