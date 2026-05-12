using EMS.Interfaces.Repositories;
using EMS.Models.Entities;
using EMS.Persistence.Context;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace EMS.Implementation.Respositories
{
    public class CartRepository : BaseRepository, ICartRepository
    {
        public CartRepository(EmsContext emsContext) : base(emsContext)
        {
        }

        public async Task<Cart?> GetCartByCustomerId(Guid customerId)
        {
            return await _emsContext.Set<Cart>()
                .Include(c => c.Items)
                .ThenInclude(i => i.Item)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);
        }

        public async Task<Guid> EnsureCartAsync(Guid customerId, CancellationToken cancellationToken)
        {
            var newCartId = NewId.Next().ToGuid();
            var now = DateTime.UtcNow;

            await _emsContext.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO Carts (Id, CustomerId, DateCreated, DateModified)
                VALUES ({newCartId}, {customerId}, {now}, {now})
                ON DUPLICATE KEY UPDATE DateModified = DateModified;",
                cancellationToken);

            return await _emsContext.Set<Cart>()
                .AsNoTracking()
                .Where(c => c.CustomerId == customerId)
                .Select(c => c.Id)
                .SingleAsync(cancellationToken);
        }

        public async Task<int?> GetCartItemQuantityAsync(Guid cartId, Guid itemId, CancellationToken cancellationToken)
        {
            return await _emsContext.Set<CartItem>()
                .AsNoTracking()
                .Where(ci => ci.CartId == cartId && ci.ItemId == itemId)
                .Select(ci => (int?)ci.Quantity)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task UpsertCartItemAsync(Guid cartId, Guid itemId, int quantity, decimal unitPrice, int maxQuantity, CancellationToken cancellationToken)
        {
            var newCartItemId = NewId.Next().ToGuid();
            var now = DateTime.UtcNow;

            await _emsContext.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO CartItems (Id, CartId, ItemId, Quantity, UnitPrice, DateCreated, DateModified)
                VALUES ({newCartItemId}, {cartId}, {itemId}, {quantity}, {unitPrice}, {now}, {now})
                ON DUPLICATE KEY UPDATE
                    Quantity = CASE
                        WHEN Quantity + {quantity} <= {maxQuantity} THEN Quantity + {quantity}
                        ELSE Quantity
                    END,
                    UnitPrice = CASE
                        WHEN Quantity + {quantity} <= {maxQuantity} THEN {unitPrice}
                        ELSE UnitPrice
                    END,
                    DateModified = CASE
                        WHEN Quantity + {quantity} <= {maxQuantity} THEN {now}
                        ELSE DateModified
                    END;",
                cancellationToken);
        }
    }
}
