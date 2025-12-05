using EMS.Models.DTOs.Customers;
using EMS.Models.DTOs.Roles;

namespace EMS.Models.DTOs.Users
{
    public class LoginRequestModel
    {
            public string Email { get; set; }
            public string Password { get; set; }
    }
    public class LoginResponseModel : BaseResponse
    {
        public string FirstName { get; set; }
        public string FullName { get; set; }
        public Guid UserId { get; set; }
        public string Email { get; set; }
        public IEnumerable<RoleDto> Roles { get; set; } = new List<RoleDto>();

        public CustomerDto Customer { get; set; }
        public AdminDto Admin { get; set; }
    }
}
