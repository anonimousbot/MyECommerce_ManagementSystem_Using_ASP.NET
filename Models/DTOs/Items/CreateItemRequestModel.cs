using Microsoft.AspNetCore.Http;

namespace EMS.Models.DTOs.Items
{
    public class CreateItemRequestModel
    {
        public string Name { get; set; }
        public string Brand { get; set; }
        public decimal Price { get; set; }
        public int QuantityInStock { get; set; }
        public IFormFile? Image { get; set; }
    }
}

