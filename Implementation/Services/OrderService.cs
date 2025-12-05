using EMS.Interfaces.Repositories;
using EMS.Interfaces.Services;
using EMS.Models.DTOs;
using EMS.Models.DTOs.Customers;
using EMS.Models.DTOs.Items;
using EMS.Models.DTOs.Orders;
using EMS.Models.Entities;
using Microsoft.VisualBasic;

namespace EMS.Implementation.Services
{
    public class OrderService(IOrderRepository orderRepository, ICustomerRepository customerRepository,
            ILogger<OrderService> logger, IUnitOfWork unitOfWork, IItemRepository itemRepository) : IOrderService
    {
        private readonly IOrderRepository _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        private readonly IItemRepository _itemRepository = itemRepository ?? throw new ArgumentNullException(nameof(itemRepository));
        private readonly ICustomerRepository _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        private readonly ILogger<OrderService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        public async Task<BaseResponse<bool>> CreateAsync(CreateOrderRequestModel model)
        {
            // Try to get customer by Customer.Id first (model.CustomerId may be a Customer.Id)
            var getCustomer = await _customerRepository.Get<Customer>(i => i.Id == model.CustomerId);

            // If not found, maybe model.CustomerId holds the User.Id (from the claim) — try UserId
            if (getCustomer == null && model.CustomerId != Guid.Empty)
            {
                getCustomer = await _customerRepository.Get<Customer>(c => c.UserId == model.CustomerId);
            }

            if (getCustomer == null)
            {
                _logger.LogError("Customer cannot be found");
                return new BaseResponse<bool>
                {
                    Message = "Customer cannot be found",
                    Status = false
                };
            }
            var getItem = await _itemRepository.Get<Item>(i => i.Id == model.ItemId);
            if (getItem == null)
            {
                _logger.LogError("Item cannot be found");
                return new BaseResponse<bool>
                {
                    Message = "Item cannot be found",
                    Status = false
                };
            }
            if (getItem.QuantityInStock < model.Quantity)
            {
                _logger.LogError("Insufficient Stock");
                return new BaseResponse<bool>
                {
                    Message = "Insufficient Stock",
                    Status = false
                };
            }
       
            var unitPrice = getItem.Price;
            var totalPrice = unitPrice + model.Quantity;


            var order = new Order
            {
                CustomerId = getCustomer.Id,
                DateCreated = DateTime.UtcNow,
                Amount = model.Amount,
                DeliveryAddress = model.DeliveryAddress,
                OrderStatus = Models.Enums.Status.Processing,
                OrderItem = new List<OrderItem>
                {
                    new OrderItem
                    {
                        ItemId = model.ItemId,
                        Quantity = model.Quantity,
                        UnitPrice = model.Amount,
                        TotalPrice = model.TotalAmount,
                    }
                }
            };
            getItem.QuantityInStock -= model.Quantity;

            var newOrder = await _orderRepository.Add(order);
            _itemRepository.Update(getItem);
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);

            if (newOrder == null)
            {
                _logger.LogError("Order Couldn't be Initialized");
                return new BaseResponse<bool>
                {
                    Message = "Order Couldn't be Initialized",
                    Status = false
                };
            }
            return new BaseResponse<bool>
            {
                Message = "Order Created Succesfully",
                Status = true
            };

        }

        public async Task<BaseResponse<bool>> DeleteAsync(Guid orderId)
        {
            var order = await _orderRepository.GetOrderById(orderId);
            if (order == null)
            {
                _logger.LogError($"Order not found.");
                return new BaseResponse<bool>
                {
                    Message = "Order not found",
                    Status = false
                };
            }

            var orderItems = order.OrderItem;
            if (orderItems != null)
            {
                foreach (var item in orderItems)
                {
                    var getItem = await _itemRepository.Get<Item>(i => i.Id == item.ItemId);

                    if (getItem != null)
                    {
                        getItem.QuantityInStock += item.Quantity;
                    }
                    _logger.LogError("Item Not Found in order");
                }
            }
            else if(orderItems == null)
            {
                _logger.LogError("Item Not Found in order");
            }
            //TRY TO ADD THE QUANTITY TO QUANTITY IN STOCK

            //var getItem = await _itemRepository.Get<Item>(i => i.Id == Item);
            //if (getItem == null)
            //{
            //    _logger.LogError("Item cannot be found");
            //    return new BaseResponse<bool>
            //    {
            //        Message = "Item cannot be found",
            //        Status = false
            //    };
            //}
            //getItem.QuantityInStock += item.Quantity;




            _orderRepository.Delete(order);
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);
            return new BaseResponse<bool>
            {
                Message = "Order Deleted Successfully",
                Status = true

            };
        }

        public async Task<BaseResponse<IReadOnlyList<OrderDto>>> GetCancelledOrderAsync(CancellationToken cancellationtoken)
        {
            var order = await _orderRepository.GetCancelledOrderAsync();
            if (!order.Any())
            {
                _logger.LogError("No Order found");
                return new BaseResponse<IReadOnlyList<OrderDto>>
                {
                    Message = "No Order Found",
                    Status = false
                };
            }
            _logger.LogInformation("Data Fetched Successfully");
            return new BaseResponse<IReadOnlyList<OrderDto>>
            {
                Message = "Data Fetched Successfully",
                Status = true,
                Data = order.Select(o => new OrderDto
                {
                    Id = o.Id,
                    Amount = o.Amount,
                    OrderStatus = o.OrderStatus,
                    OrderItem = o.OrderItem?.Select(or => new OrderItem
                    {
                        Item = or.Item,
                        Quantity = or.Quantity,
                        UnitPrice = or.UnitPrice,
                    }).ToList(),
                    //CustomerId = o.CustomerId,
                    DateCreated = o.DateCreated,
                    Customer = o.Customer
                }).ToList()

            };
        }

        public async Task<BaseResponse<IEnumerable<OrderDto>>> GetOrderAsync(CancellationToken cancellationToken)
        {
            var item = await _orderRepository.GetAllOrders();
            if (!item.Any())
            {
                _logger.LogError("No data found");
                return new BaseResponse<IEnumerable<OrderDto>>
                {
                    Message = "No data found",
                    Status = false
                };
            }
            return new BaseResponse<IEnumerable<OrderDto>>
            {
                Message = "Data fetched successfully",
                Status = true,
                Data = item.Select(i => new OrderDto
                {
                    Id = i.Id,
                    Amount = i.Amount,
                    OrderStatus = i.OrderStatus,
                    DeliveryAddress = i.DeliveryAddress,
                    OrderItem = i.OrderItem?.Select(or => new OrderItem
                    {
                        Item = or.Item,
                        Quantity = or.Quantity,
                        UnitPrice = or.UnitPrice,
                    }).ToList(),
                    DateCreated = i.DateCreated,
                    Customer = i.Customer

                }).ToList()
            };
        }

        public async Task<BaseResponse<IReadOnlyList<OrderDto>>> GetDeliveredOrderAsync(CancellationToken cancellationtoken)
        {
            var order = await _orderRepository.GetDeliveredOrderAsync();
            if (!order.Any())
            {
                _logger.LogError("No Order found");
                return new BaseResponse<IReadOnlyList<OrderDto>>
                {
                    Message = "No Order Found",
                    Status = false
                };
            }
            _logger.LogInformation("Data Fetched Successfully");
            return new BaseResponse<IReadOnlyList<OrderDto>>
            {
                Message = "Data Fetched Successfully",
                Status = true,
                Data = order.Select(o => new OrderDto
                {
                    Id = o.Id,
                    Amount = o.Amount,
                    OrderStatus = o.OrderStatus,
                    DeliveryAddress = o.DeliveryAddress,
                    OrderItem = o.OrderItem?.Select(or => new OrderItem
                    {
                        Item = or.Item,
                        Quantity = or.Quantity,
                        UnitPrice = or.UnitPrice,
                    }).ToList(),
                    //CustomerId = o.CustomerId,
                    DateCreated = o.DateCreated,
                    Customer = o.Customer
                }).ToList()


            };
        }

        public async Task<BaseResponse<OrderDto>> GetOrderById(Guid id, CancellationToken cancellationtoken)
        {
            var getOrder = await _orderRepository.GetOrderById(id);
            if (getOrder == null)
            {
                _logger.LogError("Order coudn't be found");
                return new BaseResponse<OrderDto>
                {
                    Message = "Order coudn't be found",
                    Status = false
                };
            }
            _logger.LogInformation($"Order fetched Successfully");
            return new BaseResponse<OrderDto>
            {
                Message = "Data Fetched",
                Status = true,
                Data = new OrderDto
                {
                    Id = getOrder.Id,
                    Customer = getOrder.Customer,
                    DeliveryAddress = getOrder.DeliveryAddress,
                    DateCreated = getOrder.DateCreated,
                    OrderItem = getOrder.OrderItem?.Select(i => new OrderItem 
                    {
                        Item = i.Item,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice
                    }).ToList(),
                    Amount = getOrder.Amount,
                    OrderStatus = getOrder.OrderStatus,

                }
            };
        }

        public async Task<BaseResponse<IReadOnlyList<OrderDto>>> GetPendingOrderAsync(CancellationToken cancellationtoken)
        {
            var order = await _orderRepository.GetPendingOrderAsync();
            if (!order.Any())
            {
                _logger.LogError("No Order found");
                return new BaseResponse<IReadOnlyList<OrderDto>>
                {
                    Message = "No Order Found",
                    Status = false
                };
            }
            _logger.LogInformation("Data Fetched Successfully");
            return new BaseResponse<IReadOnlyList<OrderDto>>
            {
                Message = "Data Fetched Successfully",
                Status = true,
                Data = order.Select(o => new OrderDto
                {
                    Id = o.Id,
                    Amount = o.Amount,
                    OrderStatus = o.OrderStatus,
                    DeliveryAddress = o.DeliveryAddress,
                    OrderItem = o.OrderItem?.Select(or => new OrderItem
                    {
                        Item = or.Item,
                        Quantity = or.Quantity,
                        UnitPrice = or.UnitPrice,
                    }).ToList(),
                    //CustomerId = o.CustomerId,
                    DateCreated = o.DateCreated,
                    Customer = o.Customer
                }).ToList()


            };
        }

        public async Task<BaseResponse<IReadOnlyList<OrderDto>>> GetProcessingOrderAsync(CancellationToken cancellationtoken)
        {
            var order = await _orderRepository.GetProcessingOrderAsync();
            if (!order.Any())
            {
                _logger.LogError("No Order found");
                return new BaseResponse<IReadOnlyList<OrderDto>>
                {
                    Message = "No Order Found",
                    Status = false
                };
            }
            _logger.LogInformation("Data Fetched Successfully");
            return new BaseResponse<IReadOnlyList<OrderDto>>
            {
                Message = "Data Fetched Successfully",
                Status = true,
                Data = order.Select(o => new OrderDto
                {
                    Id = o.Id,
                    Amount = o.Amount,
                    OrderStatus = o.OrderStatus,
                    OrderItem = o.OrderItem?.Select(or => new OrderItem
                    {
                        Item = or.Item,
                        Quantity = or.Quantity,
                        UnitPrice = or.UnitPrice,
                    }).ToList(),
                    //CustomerId = o.CustomerId,
                    DateCreated = o.DateCreated,
                    Customer = o.Customer
                }).ToList()

            };
        }

        public async Task<BaseResponse<IEnumerable<OrderDto>>> GetOrdersByCustomerAsync(Guid id, CancellationToken cancellationToken)
        {
            var orders = (await _orderRepository.GetOrdersByCustomerAsync(id)).ToList();

            if (!orders.Any())
            {
                var customer = await _customerRepository.Get<Customer>(c => c.UserId == id);
                if (customer != null)
                {
                    orders = (await _orderRepository.GetOrdersByCustomerAsync(customer.Id)).ToList();
                }
            }
            if (!orders.Any())
            {

                _logger.LogError("No Order found");
                return new BaseResponse<IEnumerable<OrderDto>>
                {
                    Message = "No Order Found",
                    Status = false
                };
            }
            return new BaseResponse<IEnumerable<OrderDto>>
            {
                Message = "Data fetched Successfully",
                Status = true,
                Data = orders.Select(o => new OrderDto
                {
                    Id = o.Id,
                    DateCreated = o.DateCreated,
                    DeliveryAddress = o.DeliveryAddress,
                    OrderItem = o.OrderItem.Select(or => new OrderItem
                    {
                        Item = or.Item,
                        Quantity = or.Quantity,
                        TotalPrice = or.TotalPrice,
                    }).ToList(),
                    OrderStatus = o.OrderStatus,




                }).ToList()
            };
        }

        public async Task<BaseResponse<bool>> UpdateAsync(Guid id, UpdateOrderRequestModel model)
        {
            var getOrder = await _orderRepository.GetOrderById(id);
            if (getOrder == null)
            {
                _logger.LogError("Order is not found");
                return new BaseResponse<bool>
                {
                    Message = "Order not found",
                    Status = false
                };
            }
            if (model.Status == Models.Enums.Status.Cancelled)
            {
                var orderItems = getOrder.OrderItem;
                if (orderItems != null)
                {
                    foreach (var item in orderItems)
                    {
                        var getItem = await _itemRepository.Get<Item>(i => i.Id == item.ItemId);

                        if (getItem != null)
                        {
                            getItem.QuantityInStock += item.Quantity;
                        }
                    }
                }
            }
            if (model == null)
            {
                _logger.LogError("field cannot be null");
                return new BaseResponse<bool>
                {
                    Message = "field cannot be null",
                    Status = false
                };
            }
            getOrder.OrderStatus = model.Status;
            await _unitOfWork.SaveChangesAsync();
            return new BaseResponse<bool>
            {
                Message = "order updated successfully",
                Status = true,              
            };
        }
    }
}
