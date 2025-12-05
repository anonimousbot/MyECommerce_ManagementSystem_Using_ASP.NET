using EMS.Models.DTOs.Roles;
using EMS.Models.DTOs.Customers;


namespace EMS.Models.DTOs.Users
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string PhoneNumber { get; set; }

        public string Email { get; set; }
        public ICollection<RoleDto> Roles { get; set; } = [];
        public CustomerDto Customer { get; set; }
        public AdminDto Admin { get; set; }


    }
}
