using EMS.Interfaces.Repositories;
using EMS.Interfaces.Services;
using EMS.Models.DTOs;
using EMS.Models.DTOs.Orders;
using EMS.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EMS.Implementation.Services
{
    public class OrderService(
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        ILogger<OrderService> logger,
        IUnitOfWork unitOfWork,
        IItemRepository itemRepository) : IOrderService
    {
        private readonly IOrderRepository _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        private readonly IItemRepository _itemRepository = itemRepository ?? throw new ArgumentNullException(nameof(itemRepository));
        private readonly ICustomerRepository _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        private readonly ILogger<OrderService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

        public async Task<BaseResponse<bool>> CreateAsync(CreateOrderRequestModel model)
        {
            if (model.Quantity <= 0)
            {
                return new BaseResponse<bool>
                {
                    Message = "Quantity must be greater than zero.",
                    Status = false
                };
            }

            if (string.IsNullOrWhiteSpace(model.DeliveryAddress))
            {
                return new BaseResponse<bool>
                {
                    Message = "Delivery address is required.",
                    Status = false
                };
            }

            var customer = await ResolveCustomerAsync(model.CustomerId);
            if (customer == null)
            {
                _logger.LogError("Customer cannot be found");
                return new BaseResponse<bool>
                {
                    Message = "Customer cannot be found",
                    Status = false
                };
            }

            var item = await _itemRepository.Get<Item>(i => i.Id == model.ItemId);
            if (item == null)
            {
                _logger.LogError("Item cannot be found");
                return new BaseResponse<bool>
                {
                    Message = "Item cannot be found",
                    Status = false
                };
            }

            var unitPrice = item.Price;
            var totalPrice = unitPrice * model.Quantity;

            await using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var okDecrement = await _itemRepository.TryDecrementStockAsync(item.Id, model.Quantity, CancellationToken.None);
                if (!okDecrement)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    return new BaseResponse<bool>
                    {
                        Message = "Insufficient Stock",
                        Status = false
                    };
                }

                var order = new Order
                {
                    CustomerId = customer.Id,
                    DateCreated = DateTime.UtcNow,
                    Amount = totalPrice,
                    DeliveryAddress = model.DeliveryAddress,
                    OrderStatus = Models.Enums.Status.Processing,
                    OrderItem = new List<OrderItem>
                    {
                        new()
                        {
                            ItemId = model.ItemId,
                            Quantity = model.Quantity,
                            UnitPrice = unitPrice,
                            TotalPrice = totalPrice,
                            DateCreated = DateTime.UtcNow
                        }
                    }
                };

                var newOrder = await _orderRepository.Add(order);
                await _unitOfWork.SaveChangesAsync(CancellationToken.None);

                if (newOrder == null)
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    _logger.LogError("Order couldn't be initialized");
                    return new BaseResponse<bool>
                    {
                        Message = "Order couldn't be initialized",
                        Status = false
                    };
                }

                await transaction.CommitAsync(CancellationToken.None);
                return new BaseResponse<bool>
                {
                    Message = "Order created successfully",
                    Status = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating order");
                await transaction.RollbackAsync(CancellationToken.None);
                return new BaseResponse<bool>
                {
                    Message = "Order could not be created.",
                    Status = false
                };
            }
        }

        public async Task<BaseResponse<bool>> DeleteAsync(Guid orderId)
        {
            var order = await _orderRepository.GetOrderById(orderId);
            if (order == null)
            {
                _logger.LogError("Order not found.");
                return new BaseResponse<bool>
                {
                    Message = "Order not found",
                    Status = false
                };
            }

            if (order.OrderItem != null)
            {
                foreach (var item in order.OrderItem)
                {
                    await _itemRepository.IncrementStockAsync(item.ItemId, item.Quantity, CancellationToken.None);
                }
            }

            _orderRepository.Delete(order);
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);

            return new BaseResponse<bool>
            {
                Message = "Order deleted successfully",
                Status = true
            };
        }

        public async Task<BaseResponse<IEnumerable<OrderDto>>> GetOrderAsync(CancellationToken cancellationToken, int pageNumber = 1, int pageSize = 10)
        {
            var (normalizedPageNumber, normalizedPageSize) = PaginationHelper.Normalize(pageNumber, pageSize);
            var query = _orderRepository.QueryAllOrders().OrderByDescending(o => o.DateCreated);
            var totalItems = await query.CountAsync(cancellationToken);

            if (totalItems == 0)
            {
                _logger.LogError("No data found");
                return new BaseResponse<IEnumerable<OrderDto>>
                {
                    Message = "No data found",
                    Status = false,
                    Data = [],
                    Pagination = PaginationHelper.Create(normalizedPageNumber, normalizedPageSize, totalItems)
                };
            }

            var pagedOrders = await query
                .Skip((normalizedPageNumber - 1) * normalizedPageSize)
                .Take(normalizedPageSize)
                .Select(MapOrderToDtoExpression())
                .ToListAsync(cancellationToken);

            return new BaseResponse<IEnumerable<OrderDto>>
            {
                Message = "Data fetched successfully",
                Status = true,
                Data = pagedOrders,
                Pagination = PaginationHelper.Create(normalizedPageNumber, normalizedPageSize, totalItems)
            };
        }

        public async Task<BaseResponse<IEnumerable<OrderDto>>> GetOrdersByCustomerAsync(Guid id, CancellationToken cancellationToken, int pageNumber = 1, int pageSize = 10)
        {
            var (normalizedPageNumber, normalizedPageSize) = PaginationHelper.Normalize(pageNumber, pageSize);
            var customerId = await _customerRepository.Query<Customer>()
                .AsNoTracking()
                .Where(c => c.Id == id || c.UserId == id)
                .Select(c => c.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (customerId == Guid.Empty)
            {
                _logger.LogError("Customer cannot be found");
                return new BaseResponse<IEnumerable<OrderDto>>
                {
                    Message = "Customer cannot be found",
                    Status = false,
                    Data = [],
                    Pagination = PaginationHelper.Create(normalizedPageNumber, normalizedPageSize, 0)
                };
            }

            var query = _orderRepository.QueryOrdersByCustomer(customerId).OrderByDescending(o => o.DateCreated);
            var totalItems = await query.CountAsync(cancellationToken);

            if (totalItems == 0)
            {
                _logger.LogError("No Order found");
                return new BaseResponse<IEnumerable<OrderDto>>
                {
                    Message = "No Order Found",
                    Status = false,
                    Data = [],
                    Pagination = PaginationHelper.Create(normalizedPageNumber, normalizedPageSize, 0)
                };
            }

            var pagedOrders = await query
                .Skip((normalizedPageNumber - 1) * normalizedPageSize)
                .Take(normalizedPageSize)
                .Select(MapOrderToDtoExpression())
                .ToListAsync(cancellationToken);

            return new BaseResponse<IEnumerable<OrderDto>>
            {
                Message = "Data fetched Successfully",
                Status = true,
                Data = pagedOrders,
                Pagination = PaginationHelper.Create(normalizedPageNumber, normalizedPageSize, totalItems)
            };
        }

        public async Task<BaseResponse<OrderDto>> GetOrderById(Guid id, CancellationToken cancellationtoken)
        {
            var order = await _orderRepository.GetOrderById(id);
            if (order == null)
            {
                _logger.LogError("Order couldn't be found");
                return new BaseResponse<OrderDto>
                {
                    Message = "Order couldn't be found",
                    Status = false
                };
            }

            return new BaseResponse<OrderDto>
            {
                Message = "Data fetched",
                Status = true,
                Data = MapOrderToDto(order)
            };
        }

        public async Task<BaseResponse<IReadOnlyList<OrderDto>>> GetPendingOrderAsync(CancellationToken cancellationtoken)
        {
            var orders = await _orderRepository.GetPendingOrderAsync();
            return MapOrderListResponse(orders, "No Order Found");
        }

        public async Task<BaseResponse<IReadOnlyList<OrderDto>>> GetProcessingOrderAsync(CancellationToken cancellationtoken)
        {
            var orders = await _orderRepository.GetProcessingOrderAsync();
            return MapOrderListResponse(orders, "No Order Found");
        }

        public async Task<BaseResponse<IReadOnlyList<OrderDto>>> GetDeliveredOrderAsync(CancellationToken cancellationtoken)
        {
            var orders = await _orderRepository.GetDeliveredOrderAsync();
            return MapOrderListResponse(orders, "No Order Found");
        }

        public async Task<BaseResponse<IReadOnlyList<OrderDto>>> GetCancelledOrderAsync(CancellationToken cancellationtoken)
        {
            var orders = await _orderRepository.GetCancelledOrderAsync();
            return MapOrderListResponse(orders, "No Order Found");
        }

        public async Task<BaseResponse<bool>> UpdateAsync(Guid id, UpdateOrderRequestModel model)
        {
            var order = await _orderRepository.GetOrderById(id);
            if (order == null)
            {
                _logger.LogError("Order is not found");
                return new BaseResponse<bool>
                {
                    Message = "Order not found",
                    Status = false
                };
            }

            if (model.Status == Models.Enums.Status.Cancelled && order.OrderItem != null)
            {
                foreach (var item in order.OrderItem)
                {
                    await _itemRepository.IncrementStockAsync(item.ItemId, item.Quantity, CancellationToken.None);
                }
            }

            order.OrderStatus = model.Status;
            await _unitOfWork.SaveChangesAsync();

            return new BaseResponse<bool>
            {
                Message = "Order updated successfully",
                Status = true,
            };
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

        private BaseResponse<IReadOnlyList<OrderDto>> MapOrderListResponse(IEnumerable<Order> orders, string emptyMessage)
        {
            var mappedOrders = orders.Select(MapOrderToDto).ToList();
            if (mappedOrders.Count == 0)
            {
                _logger.LogError("No Order found");
                return new BaseResponse<IReadOnlyList<OrderDto>>
                {
                    Message = emptyMessage,
                    Status = false
                };
            }

            return new BaseResponse<IReadOnlyList<OrderDto>>
            {
                Message = "Data Fetched Successfully",
                Status = true,
                Data = mappedOrders
            };
        }

        private static OrderDto MapOrderToDto(Order order)
        {
            return new OrderDto
            {
                Id = order.Id,
                DateCreated = order.DateCreated,
                DateModified = order.DateModified,
                DeliveryAddress = order.DeliveryAddress,
                CustomerId = order.CustomerId,
                Customer = order.Customer,
                Amount = order.Amount,
                OrderStatus = order.OrderStatus,
                OrderItem = order.OrderItem?.Select(or => new OrderItem
                {
                    Id = or.Id,
                    ItemId = or.ItemId,
                    Item = or.Item,
                    Quantity = or.Quantity,
                    UnitPrice = or.UnitPrice,
                    TotalPrice = or.TotalPrice,
                    DateCreated = or.DateCreated,
                    DateModified = or.DateModified,
                    OrderId = or.OrderId
                }).ToList()
            };
        }

        private static System.Linq.Expressions.Expression<Func<Order, OrderDto>> MapOrderToDtoExpression()
        {
            return order => new OrderDto
            {
                Id = order.Id,
                DateCreated = order.DateCreated,
                DateModified = order.DateModified,
                DeliveryAddress = order.DeliveryAddress,
                CustomerId = order.CustomerId,
                Customer = order.Customer,
                Amount = order.Amount,
                OrderStatus = order.OrderStatus,
                OrderItem = order.OrderItem!.Select(or => new OrderItem
                {
                    Id = or.Id,
                    ItemId = or.ItemId,
                    Item = or.Item,
                    Quantity = or.Quantity,
                    UnitPrice = or.UnitPrice,
                    TotalPrice = or.TotalPrice,
                    DateCreated = or.DateCreated,
                    DateModified = or.DateModified,
                    OrderId = or.OrderId
                }).ToList()
            };
        }
    }
}
