using EMS.Models.Entities;
using EMS.Models.Enums;

namespace EMS.Models.DTOs.Customers
{
    public class CreateCustomerRequestModel
    {
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string ConfirmPassword { get; set; }

        //public Guid UserId { get; set; }
        //public User User { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Gender Gender { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        //public List<Guid> RoleIds { get; set; } = [];
    }
}
