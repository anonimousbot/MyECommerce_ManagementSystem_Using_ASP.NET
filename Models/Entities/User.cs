using System.Numerics;
using EMS.Contracts.Entities;

namespace EMS.Models.Entities
{
    public class User : BaseEntity
    {
        public string Email { get; set; }
        public bool EmailConfirmed { get; set; }
        public string? PasswordHash { get; set; }
        public Customer? Customer { get; set; }
        public string? GoogleId { get; set; }

        public Admin? Admin { get; set; }

        public ICollection<UserRole> UserRoles { get; set; } = [];


        public string ChangePassword(string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword))
            {
                throw new ArgumentException("Password cannot be empty", nameof(newPassword));
            }
            PasswordHash = newPassword;
            return PasswordHash;
        }
    }
}
