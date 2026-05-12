using System.Text.Json;
using EMS.Interfaces.Repositories;
using EMS.Interfaces.Services;
using EMS.Models.DTOs;
using EMS.Models.DTOs.Payments;
using EMS.Models.Entities;
using EMS.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace EMS.Implementation.Services
{
    public class PaymentService(
        IPaymentRepository paymentRepository,
        IOrderRepository orderRepository,
        IItemRepository itemRepository,
        ICartRepository cartRepository,
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork,
        IPaystackClient paystackClient,
        ILogger<PaymentService> logger) : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
        private readonly IOrderRepository _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        private readonly IItemRepository _itemRepository = itemRepository ?? throw new ArgumentNullException(nameof(itemRepository));
        private readonly ICartRepository _cartRepository = cartRepository ?? throw new ArgumentNullException(nameof(cartRepository));
        private readonly ICustomerRepository _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        private readonly IPaystackClient _paystackClient = paystackClient ?? throw new ArgumentNullException(nameof(paystackClient));
        private readonly ILogger<PaymentService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public async Task<BaseResponse<PaystackInitializeResultDto>> InitializePaystackAsync(
            Guid customerId,
            string email,
            decimal amount,
            string callbackUrl,
            string checkoutSnapshotJson,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return new BaseResponse<PaystackInitializeResultDto> { Status = false, Message = "Email is required." };
            }
            if (string.IsNullOrWhiteSpace(callbackUrl))
            {
                return new BaseResponse<PaystackInitializeResultDto> { Status = false, Message = "Callback URL is required." };
            }
            if (string.IsNullOrWhiteSpace(checkoutSnapshotJson))
            {
                return new BaseResponse<PaystackInitializeResultDto> { Status = false, Message = "Checkout snapshot is required." };
            }

            var amountKobo = (int)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
            if (amountKobo <= 0)
            {
                return new BaseResponse<PaystackInitializeResultDto> { Status = false, Message = "Invalid amount." };
            }

            var reference = $"EMS-{Guid.NewGuid():N}";

            var payment = new Payment
            {
                CustomerId = customerId,
                Amount = amount,
                AmountKobo = amountKobo,
                Currency = "NGN",
                Email = email,
                Reference = reference,
                CheckoutSnapshotJson = checkoutSnapshotJson,
                Status = PaymentStatus.Initialized,
                DateCreated = DateTime.UtcNow
            };

            await _paymentRepository.Add(payment);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var (ok, message, authorizationUrl, returnedReference) = await _paystackClient.InitializeAsync(
                email,
                amountKobo,
                callbackUrl,
                reference,
                new { customerId },
                cancellationToken);

            if (!ok || string.IsNullOrWhiteSpace(authorizationUrl) || string.IsNullOrWhiteSpace(returnedReference))
            {
                _logger.LogError("Paystack init failed: {Message}", message);
                payment.Status = PaymentStatus.Failed;
                payment.DateModified = DateTime.UtcNow;
                _paymentRepository.Update(payment);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return new BaseResponse<PaystackInitializeResultDto> { Status = false, Message = message };
            }

            if (!string.Equals(returnedReference, reference, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("Paystack returned unexpected reference. Expected {Expected}, got {Got}", reference, returnedReference);
                return new BaseResponse<PaystackInitializeResultDto> { Status = false, Message = "Payment reference mismatch." };
            }

            return new BaseResponse<PaystackInitializeResultDto>
            {
                Status = true,
                Message = "Payment initialized",
                Data = new PaystackInitializeResultDto
                {
                    AuthorizationUrl = authorizationUrl,
                    Reference = reference
                }
            };
        }

        public async Task<BaseResponse<PaymentVerificationResultDto>> VerifyPaystackForUserAsync(Guid userId, string reference, CancellationToken cancellationToken)
        {
            var payment = await _paymentRepository.GetByReferenceAsync(reference);
            if (payment == null)
            {
                return new BaseResponse<PaymentVerificationResultDto> { Status = false, Message = "Payment reference not found." };
            }

            var customer = await ResolveCustomerAsync(userId);
            if (customer == null || payment.CustomerId != customer.Id)
            {
                return new BaseResponse<PaymentVerificationResultDto> { Status = false, Message = "You are not allowed to verify this payment." };
            }

            return await VerifyPaystackAsync(reference, cancellationToken);
        }

        public async Task<BaseResponse<PaymentVerificationResultDto>> VerifyPaystackAsync(string reference, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(reference))
            {
                return new BaseResponse<PaymentVerificationResultDto> { Status = false, Message = "Missing payment reference." };
            }

            var payment = await _paymentRepository.GetByReferenceAsync(reference);
            if (payment == null)
            {
                return new BaseResponse<PaymentVerificationResultDto> { Status = false, Message = "Payment reference not found." };
            }

            // Idempotent: already successful + linked order
            if (payment.Status == PaymentStatus.Successful && payment.OrderId != null)
            {
                return new BaseResponse<PaymentVerificationResultDto>
                {
                    Status = true,
                    Message = "Payment already verified.",
                    Data = new PaymentVerificationResultDto
                    {
                        Paid = true,
                        Reference = payment.Reference,
                        Message = "Payment already verified.",
                        OrderId = payment.OrderId
                    }
                };
            }

            var (ok, message, paid, amountKobo, currency) = await _paystackClient.VerifyAsync(reference, cancellationToken);
            if (!ok)
            {
                _logger.LogError("Paystack verify failed: {Message}", message);
                return new BaseResponse<PaymentVerificationResultDto> { Status = false, Message = message };
            }

            if (amountKobo.HasValue && payment.AmountKobo > 0 && amountKobo.Value != payment.AmountKobo)
            {
                _logger.LogError("Paystack amount mismatch for {Reference}. Expected {Expected}, got {Got}", reference, payment.AmountKobo, amountKobo.Value);
                return new BaseResponse<PaymentVerificationResultDto> { Status = false, Message = "Payment amount mismatch." };
            }
            if (!string.IsNullOrWhiteSpace(currency) && !string.Equals(currency, payment.Currency, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("Paystack currency mismatch for {Reference}. Expected {Expected}, got {Got}", reference, payment.Currency, currency);
                return new BaseResponse<PaymentVerificationResultDto> { Status = false, Message = "Payment currency mismatch." };
            }

            if (!paid)
            {
                payment.Status = PaymentStatus.Failed;
                payment.DateModified = DateTime.UtcNow;
                _paymentRepository.Update(payment);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return new BaseResponse<PaymentVerificationResultDto>
                {
                    Status = true,
                    Message = "Payment not successful.",
                    Data = new PaymentVerificationResultDto
                    {
                        Paid = false,
                        Reference = payment.Reference,
                        Message = "Payment not successful.",
                        OrderId = payment.OrderId
                    }
                };
            }

            // Fulfill: create order from snapshot + decrement stock atomically
            try
            {
                var tx = await _unitOfWork.BeginTransactionAsync();
                var txDisposed = false;
                try
                {
                    // Reload inside the transaction to get latest state
                    payment = await _paymentRepository.GetByReferenceAsync(reference) ?? payment;

                    // If an order already exists for this payment reference, link and exit idempotently
                    var existingOrder = await _orderRepository.GetOrderByPaymentReferenceAsync(reference);
                    if (existingOrder != null)
                    {
                        payment.OrderId = existingOrder.Id;
                        payment.Status = PaymentStatus.Successful;
                        payment.PaidAt ??= DateTime.UtcNow;
                        payment.DateModified = DateTime.UtcNow;
                        _paymentRepository.Update(payment);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        await tx.CommitAsync(cancellationToken);

                        return new BaseResponse<PaymentVerificationResultDto>
                        {
                            Status = true,
                            Message = "Payment successful.",
                            Data = new PaymentVerificationResultDto
                            {
                                Paid = true,
                                Reference = reference,
                                Message = "Payment successful.",
                                OrderId = existingOrder.Id
                            }
                        };
                    }

                    var snapshot = JsonSerializer.Deserialize<CheckoutSnapshotDto>(payment.CheckoutSnapshotJson);
                    if (snapshot == null || snapshot.Items.Count == 0 || string.IsNullOrWhiteSpace(snapshot.DeliveryAddress))
                    {
                        payment.Status = PaymentStatus.SuccessfulButUnfulfilled;
                        payment.PaidAt ??= DateTime.UtcNow;
                        payment.DateModified = DateTime.UtcNow;
                        _paymentRepository.Update(payment);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        await tx.CommitAsync(cancellationToken);

                        return new BaseResponse<PaymentVerificationResultDto>
                        {
                            Status = true,
                            Message = "Payment successful, but we couldn't create your order. Please contact support.",
                            Data = new PaymentVerificationResultDto
                            {
                                Paid = true,
                                Reference = reference,
                                Message = "Payment successful, but we couldn't create your order. Please contact support.",
                                OrderId = null
                            }
                        };
                    }

                    var orderItems = new List<OrderItem>();
                    decimal computedTotal = 0m;

                    foreach (var snapshotItem in snapshot.Items)
                    {
                        if (snapshotItem.Quantity <= 0 || snapshotItem.UnitPrice <= 0)
                        {
                            payment.Status = PaymentStatus.SuccessfulButUnfulfilled;
                            payment.PaidAt ??= DateTime.UtcNow;
                            payment.DateModified = DateTime.UtcNow;
                            _paymentRepository.Update(payment);
                            await _unitOfWork.SaveChangesAsync(cancellationToken);
                            await tx.CommitAsync(cancellationToken);

                            return new BaseResponse<PaymentVerificationResultDto>
                            {
                                Status = true,
                                Message = "Payment successful, but order items were invalid. Please contact support.",
                                Data = new PaymentVerificationResultDto
                                {
                                    Paid = true,
                                    Reference = reference,
                                    Message = "Payment successful, but order items were invalid. Please contact support.",
                                    OrderId = null
                                }
                            };
                        }

                        var okDecrement = await _itemRepository.TryDecrementStockAsync(snapshotItem.ItemId, snapshotItem.Quantity, cancellationToken);
                        if (!okDecrement)
                        {
                            await tx.RollbackAsync(cancellationToken);
                            await tx.DisposeAsync();
                            txDisposed = true;
                            await MarkUnfulfilledAsync(payment, cancellationToken);

                            return new BaseResponse<PaymentVerificationResultDto>
                            {
                                Status = true,
                                Message = "Payment successful, but stock is no longer available for one or more items. Please contact support.",
                                Data = new PaymentVerificationResultDto
                                {
                                    Paid = true,
                                    Reference = reference,
                                    Message = "Payment successful, but stock is no longer available for one or more items. Please contact support.",
                                    OrderId = null
                                }
                            };
                        }

                        var lineTotal = snapshotItem.UnitPrice * snapshotItem.Quantity;
                        computedTotal += lineTotal;

                        orderItems.Add(new OrderItem
                        {
                            ItemId = snapshotItem.ItemId,
                            Quantity = snapshotItem.Quantity,
                            UnitPrice = snapshotItem.UnitPrice,
                            TotalPrice = lineTotal,
                            DateCreated = DateTime.UtcNow
                        });
                    }

                    var computedKobo = (int)Math.Round(computedTotal * 100m, MidpointRounding.AwayFromZero);
                    if (payment.AmountKobo > 0 && computedKobo != payment.AmountKobo)
                    {
                        await tx.RollbackAsync(cancellationToken);
                        await tx.DisposeAsync();
                        txDisposed = true;
                        await MarkUnfulfilledAsync(payment, cancellationToken);

                        return new BaseResponse<PaymentVerificationResultDto>
                        {
                            Status = true,
                            Message = "Payment successful, but amount verification failed. Please contact support.",
                            Data = new PaymentVerificationResultDto
                            {
                                Paid = true,
                                Reference = reference,
                                Message = "Payment successful, but amount verification failed. Please contact support.",
                                OrderId = null
                            }
                        };
                    }

                    var order = new Order
                    {
                        CustomerId = payment.CustomerId,
                        DeliveryAddress = snapshot.DeliveryAddress,
                        Amount = computedTotal,
                        OrderStatus = Status.Processing,
                        PaymentReference = reference,
                        DateCreated = DateTime.UtcNow,
                        OrderItem = orderItems
                    };

                    await _orderRepository.Add(order);

                    payment.OrderId = order.Id;
                    payment.Status = PaymentStatus.Successful;
                    payment.PaidAt ??= DateTime.UtcNow;
                    payment.DateModified = DateTime.UtcNow;
                    _paymentRepository.Update(payment);

                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    // Clear cart after successful fulfillment (best-effort)
                    var cart = await _cartRepository.GetCartByCustomerId(payment.CustomerId);
                    if (cart != null && cart.Items.Count > 0)
                    {
                        foreach (var ci in cart.Items.ToList())
                        {
                            _cartRepository.Delete(ci);
                        }
                        cart.Items.Clear();
                        cart.DateModified = DateTime.UtcNow;
                        _cartRepository.Update(cart);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                    }

                    await tx.CommitAsync(cancellationToken);

                    return new BaseResponse<PaymentVerificationResultDto>
                    {
                        Status = true,
                        Message = "Payment successful.",
                        Data = new PaymentVerificationResultDto
                        {
                            Paid = true,
                            Reference = reference,
                            Message = "Payment successful.",
                            OrderId = order.Id
                        }
                    };
                }
                finally
                {
                    if (!txDisposed)
                    {
                        await tx.DisposeAsync();
                    }
                }
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DbUpdateException during payment fulfillment for {Reference}", reference);

                // If unique constraint hit (order already created), fetch it and return idempotently.
                var existingOrder = await _orderRepository.GetOrderByPaymentReferenceAsync(reference);
                if (existingOrder != null)
                {
                    try
                    {
                        var p = await _paymentRepository.GetByReferenceAsync(reference);
                        if (p != null && (p.OrderId == null || p.Status != PaymentStatus.Successful))
                        {
                            p.OrderId = existingOrder.Id;
                            p.Status = PaymentStatus.Successful;
                            p.PaidAt ??= DateTime.UtcNow;
                            p.DateModified = DateTime.UtcNow;
                            _paymentRepository.Update(p);
                            await _unitOfWork.SaveChangesAsync(cancellationToken);
                        }
                    }
                    catch
                    {
                        // Best-effort only; order already exists.
                    }

                    return new BaseResponse<PaymentVerificationResultDto>
                    {
                        Status = true,
                        Message = "Payment successful.",
                        Data = new PaymentVerificationResultDto
                        {
                            Paid = true,
                            Reference = reference,
                            Message = "Payment successful.",
                            OrderId = existingOrder.Id
                        }
                    };
                }

                return new BaseResponse<PaymentVerificationResultDto> { Status = false, Message = "Could not create order after payment. Please contact support." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during payment fulfillment for {Reference}", reference);
                return new BaseResponse<PaymentVerificationResultDto> { Status = false, Message = "Unexpected error verifying payment." };
            }
        }

        private async Task<Customer?> ResolveCustomerAsync(Guid customerOrUserId)
        {
            // Try customerId
            var customer = await _customerRepository.Get<Customer>(c => c.Id == customerOrUserId);
            if (customer != null)
            {
                return customer;
            }

            // Try userId
            return await _customerRepository.Get<Customer>(c => c.UserId == customerOrUserId);
        }

        private async Task MarkUnfulfilledAsync(Payment payment, CancellationToken cancellationToken)
        {
            payment.Status = PaymentStatus.SuccessfulButUnfulfilled;
            payment.PaidAt ??= DateTime.UtcNow;
            payment.DateModified = DateTime.UtcNow;
            _paymentRepository.Update(payment);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
