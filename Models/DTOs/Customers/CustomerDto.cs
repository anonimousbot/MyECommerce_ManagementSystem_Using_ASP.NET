using System.ComponentModel.DataAnnotations;
using EMS.Models.Entities;
using EMS.Models.Enums;

namespace EMS.Models.DTOs.Customers
{
    public class CustomerDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; }
        public ICollection<Order>? Orders { get; set; } = [];
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Gender? Gender { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public string FullName { get; set; }

    }
}
