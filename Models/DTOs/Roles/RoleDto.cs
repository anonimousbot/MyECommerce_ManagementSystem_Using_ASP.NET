using EMS.Models.Entities;

namespace EMS.Models.DTOs.Roles
{
    public class RoleDto
    {
        public Guid Id { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ICollection<UserRole> UserRoles { get; set; } = [];
    }
}
