using EMS.Contracts.Entities;

namespace EMS.Models.Entities
{
    public class Customer : BaseUser
    {
        public Guid UserId {  get; set; }
        public User User { get; set; }

        // Collection Of Order
        public ICollection<Order>? Orders { get; set; }
    }
}
