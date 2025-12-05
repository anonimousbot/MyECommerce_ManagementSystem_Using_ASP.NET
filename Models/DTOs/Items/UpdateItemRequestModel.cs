namespace EMS.Models.DTOs.Items
{
    public class UpdateItemRequestModel
    {
       
        public string Name { get; set; }
        public string Brand { get; set; }
        public decimal Price { get; set; }
        public int QuantityInStock { get; set; }
       
    }
}
