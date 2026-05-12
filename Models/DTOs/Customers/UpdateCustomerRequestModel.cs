using EMS.Models.Enums;

namespace EMS.Models.DTOs.Customers
{
    public class UpdateCustomerRequestModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Gender? Gender { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
    }
}
