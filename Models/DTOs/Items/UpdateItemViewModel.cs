using Microsoft.AspNetCore.Http;

namespace EMS.Models.DTOs.Items
{
    public class UpdateItemViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int QuantityInStock { get; set; }
        public IFormFile? Image { get; set; }
        public string? CurrentImagePath { get; set; }
    }
}
