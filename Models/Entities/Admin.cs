using EMS.Contracts.Entities;

namespace EMS.Models.Entities
{
    public class Admin : BaseUser
    {
        public Guid UserId { get; set; }
        public User User { get; set; }
    }
}