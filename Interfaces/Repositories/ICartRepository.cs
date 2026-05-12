using EMS.Models.Entities;

namespace EMS.Interfaces.Repositories
{
    public interface ICartRepository : IBaseRepository
    {
        Task<Cart?> GetCartByCustomerId(Guid customerId);
        Task<Guid> EnsureCartAsync(Guid customerId, CancellationToken cancellationToken);
        Task<int?> GetCartItemQuantityAsync(Guid cartId, Guid itemId, CancellationToken cancellationToken);
        Task UpsertCartItemAsync(Guid cartId, Guid itemId, int quantity, decimal unitPrice, int maxQuantity, CancellationToken cancellationToken);
    }
}
