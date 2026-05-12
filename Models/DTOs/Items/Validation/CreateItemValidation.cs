using EMS.Models.DTOs.Items;
using FluentValidation;
using System.IO;

namespace EMS.Models.DTOs.Items.Validation
{
    public class CreateItemValidation : AbstractValidator<CreateItemRequestModel>
    {
        public CreateItemValidation()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
            RuleFor(x => x.Brand).NotEmpty().WithMessage("BrandName is required");
            RuleFor(x => x.Price).GreaterThan(0).WithMessage("Price must be greater than 0");
            RuleFor(x => x.QuantityInStock).GreaterThanOrEqualTo(0).WithMessage("Quantity must be 0 or more");

            RuleFor(x => x.Image)
                .Must(f => f == null || f.Length <= 5 * 1024 * 1024)
                .WithMessage("Image must be 5MB or smaller");

            RuleFor(x => x.Image)
                .Must(f =>
                {
                    if (f == null) return true;
                    var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
                    var ext = Path.GetExtension(f.FileName)?.ToLowerInvariant();
                    return !string.IsNullOrWhiteSpace(ext) && allowed.Contains(ext);
                })
                .WithMessage("Image must be JPG, PNG, WEBP, or GIF");
        }
    }
}
