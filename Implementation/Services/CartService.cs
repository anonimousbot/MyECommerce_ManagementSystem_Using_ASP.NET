using EMS.Interfaces.Repositories;
using EMS.Interfaces.Services;
using EMS.Models.DTOs;
using EMS.Models.DTOs.Carts;
using EMS.Models.DTOs.Payments;
using EMS.Models.Entities;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace EMS.Implementation.Services
{
    public class CartService(
        ICartRepository cartRepository,
        ICustomerRepository customerRepository,
        IItemRepository itemRepository,
        IUnitOfWork unitOfWork,
        IPaymentService paymentService) : ICartService
    {
        private readonly ICartRepository _cartRepository = cartRepository ?? throw new ArgumentNullException(nameof(cartRepository));
        private readonly ICustomerRepository _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        private readonly IItemRepository _itemRepository = itemRepository ?? throw new ArgumentNullException(nameof(itemRepository));
        private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        private readonly IPaymentService _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));

        public async Task<BaseResponse<CartDto>> GetCartAsync(Guid userId, CancellationToken cancellationToken)
        {
            var customer = await ResolveCustomerAsync(userId);
            if (customer == null)
            {
                return new BaseResponse<CartDto> { Status = false, Message = "Customer cannot be found." };
            }

            var cart = await _cartRepository.GetCartByCustomerId(customer.Id);
            cart ??= new Cart { CustomerId = customer.Id, DateCreated = DateTime.UtcNow };

            var items = (cart.Items ?? []).Select(ci => new CartItemDto
            {
                ItemId = ci.ItemId,
                Name = ci.Item?.Name ?? string.Empty,
                Brand = ci.Item?.Brand ?? string.Empty,
                UnitPrice = ci.UnitPrice,
                Quantity = ci.Quantity
            }).ToList();

            return new BaseResponse<CartDto>
            {
                Status = true,
                Message = "Cart fetched",
                Data = new CartDto
                {
                    CustomerId = customer.Id,
                    Items = items
                }
            };
        }

        public async Task<BaseResponse<bool>> AddItemAsync(Guid userId, Guid itemId, int quantity, CancellationToken cancellationToken)
        {
            if (quantity <= 0)
            {
                return new BaseResponse<bool> { Status = false, Message = "Quantity must be greater than 0." };
            }

            var customer = await ResolveCustomerAsync(userId);
            if (customer == null)
            {
                return new BaseResponse<bool> { Status = false, Message = "Customer cannot be found." };
            }

            var item = await _itemRepository.Get<Item>(i => i.Id == itemId);
            if (item == null)
            {
                return new BaseResponse<bool> { Status = false, Message = "Item not found." };
            }

            try
            {
                if (item.QuantityInStock < quantity)
                {
                    return new BaseResponse<bool> { Status = false, Message = "Insufficient stock for requested quantity." };
                }

                var cartId = await _cartRepository.EnsureCartAsync(customer.Id, cancellationToken);
                var existingQty = await _cartRepository.GetCartItemQuantityAsync(cartId, itemId, cancellationToken) ?? 0;
                var targetQty = existingQty + quantity;
                if (item.QuantityInStock < targetQty)
                {
                    return new BaseResponse<bool> { Status = false, Message = "Insufficient stock for requested quantity." };
                }

                await _cartRepository.UpsertCartItemAsync(cartId, item.Id, quantity, item.Price, item.QuantityInStock, cancellationToken);
                var refreshedQty = await _cartRepository.GetCartItemQuantityAsync(cartId, itemId, cancellationToken) ?? 0;
                if (refreshedQty < targetQty)
                {
                    return new BaseResponse<bool> { Status = false, Message = "Insufficient stock for requested quantity." };
                }

                return new BaseResponse<bool> { Status = true, Message = "Item added to cart.", Data = true };
            }
            catch (DbUpdateException)
            {
                return new BaseResponse<bool>
                {
                    Status = false,
                    Message = "Could not update your cart right now. Please try again."
                };
            }
        }

        public async Task<BaseResponse<bool>> UpdateItemQuantityAsync(Guid userId, Guid itemId, int quantity, CancellationToken cancellationToken)
        {
            if (quantity <= 0)
            {
                return await RemoveItemAsync(userId, itemId, cancellationToken);
            }

            var customer = await ResolveCustomerAsync(userId);
            if (customer == null)
            {
                return new BaseResponse<bool> { Status = false, Message = "Customer cannot be found." };
            }

            var cart = await _cartRepository.GetCartByCustomerId(customer.Id);
            if (cart == null)
            {
                return new BaseResponse<bool> { Status = false, Message = "Cart not found." };
            }

            var item = await _itemRepository.Get<Item>(i => i.Id == itemId);
            if (item == null)
            {
                return new BaseResponse<bool> { Status = false, Message = "Item not found." };
            }

            if (item.QuantityInStock < quantity)
            {
                return new BaseResponse<bool> { Status = false, Message = "Insufficient stock for requested quantity." };
            }

            var existing = cart.Items.FirstOrDefault(i => i.ItemId == itemId);
            if (existing == null)
            {
                return new BaseResponse<bool> { Status = false, Message = "Item not found in cart." };
            }

            existing.Quantity = quantity;
            existing.UnitPrice = item.Price;
            existing.DateModified = DateTime.UtcNow;
            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return new BaseResponse<bool>
                {
                    Status = false,
                    Message = "Your cart changed due to another request. Please refresh and try again."
                };
            }
            return new BaseResponse<bool> { Status = true, Message = "Cart updated.", Data = true };
        }

        public async Task<BaseResponse<bool>> RemoveItemAsync(Guid userId, Guid itemId, CancellationToken cancellationToken)
        {
            var customer = await ResolveCustomerAsync(userId);
            if (customer == null)
            {
                return new BaseResponse<bool> { Status = false, Message = "Customer cannot be found." };
            }

            var cart = await _cartRepository.GetCartByCustomerId(customer.Id);
            if (cart == null)
            {
                return new BaseResponse<bool> { Status = false, Message = "Cart not found." };
            }

            var existing = cart.Items.FirstOrDefault(i => i.ItemId == itemId);
            if (existing == null)
            {
                return new BaseResponse<bool> { Status = true, Message = "Item removed.", Data = true };
            }

            cart.Items.Remove(existing);
            _cartRepository.Delete(existing);
            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return new BaseResponse<bool>
                {
                    Status = false,
                    Message = "This cart item was already updated/removed. Please refresh your cart."
                };
            }
            return new BaseResponse<bool> { Status = true, Message = "Item removed.", Data = true };
        }

        public async Task<BaseResponse<PaystackInitializeResultDto>> CheckoutAsync(Guid userId, string email, string deliveryAddress, string callbackUrl, CancellationToken cancellationToken)
        {
            var customer = await ResolveCustomerAsync(userId);
            if (customer == null)
            {
                return new BaseResponse<PaystackInitializeResultDto> { Status = false, Message = "Customer cannot be found." };
            }

            var cart = await _cartRepository.GetCartByCustomerId(customer.Id);
            if (cart == null || cart.Items.Count == 0)
            {
                return new BaseResponse<PaystackInitializeResultDto> { Status = false, Message = "Cart is empty." };
            }

            if (string.IsNullOrWhiteSpace(deliveryAddress))
            {
                return new BaseResponse<PaystackInitializeResultDto> { Status = false, Message = "Delivery address is required." };
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                return new BaseResponse<PaystackInitializeResultDto> { Status = false, Message = "Customer email not found." };
            }

            // Validate stock and compute total (server-side)
            decimal total = 0m;
            var snapshotItems = new List<CheckoutSnapshotItemDto>();
            var unitPriceChanged = false;

            foreach (var cartItem in cart.Items)
            {
                var item = cartItem.Item ?? await _itemRepository.Get<Item>(i => i.Id == cartItem.ItemId);
                if (item == null)
                {
                    return new BaseResponse<PaystackInitializeResultDto> { Status = false, Message = "An item in your cart no longer exists." };
                }

                if (item.QuantityInStock < cartItem.Quantity)
                {
                    return new BaseResponse<PaystackInitializeResultDto> { Status = false, Message = $"Insufficient stock for {item.Name}." };
                }

                var unitPrice = item.Price;
                var lineTotal = unitPrice * cartItem.Quantity;
                total += lineTotal;

                snapshotItems.Add(new CheckoutSnapshotItemDto
                {
                    ItemId = item.Id,
                    Quantity = cartItem.Quantity,
                    UnitPrice = unitPrice,
                });

                if (cartItem.UnitPrice != unitPrice)
                {
                    cartItem.UnitPrice = unitPrice;
                    cartItem.DateModified = DateTime.UtcNow;
                    unitPriceChanged = true;
                }
            }

            if (unitPriceChanged)
            {
                try
                {
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateConcurrencyException)
                {
                    return new BaseResponse<PaystackInitializeResultDto>
                    {
                        Status = false,
                        Message = "Cart changed while preparing checkout. Please review your cart and try again."
                    };
                }
            }

            var snapshot = new CheckoutSnapshotDto
            {
                DeliveryAddress = deliveryAddress,
                Items = snapshotItems
            };
            var snapshotJson = JsonSerializer.Serialize(snapshot);

            return await _paymentService.InitializePaystackAsync(customer.Id, email, total, callbackUrl, snapshotJson, cancellationToken);
        }

        private async Task<Customer?> ResolveCustomerAsync(Guid customerOrUserId)
        {
            var customer = await _customerRepository.Get<Customer>(c => c.Id == customerOrUserId);
            if (customer != null)
            {
                return customer;
            }

            return await _customerRepository.Get<Customer>(c => c.UserId == customerOrUserId);
        }
    }
}
