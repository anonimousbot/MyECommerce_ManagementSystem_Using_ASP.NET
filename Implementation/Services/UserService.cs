using EMS.Interfaces.Repositories;
using EMS.Interfaces.Services;
using EMS.Models.DTOs;
using EMS.Models.DTOs.Roles;
using EMS.Models.DTOs.Users;
using EMS.Models.Entities;
using Microsoft.AspNetCore.Identity;

namespace EMS.Implementation.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<UserService> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICustomerService _customerService;

        public UserService(IUserRepository userRepository,
            UserManager<User> userManager,
            ILogger<UserService> logger,
            IUnitOfWork unitOfWork,
            ICustomerService customerService
            )
        {
            _userRepository = userRepository;
            _userManager = userManager;
            _logger = logger;
            _unitOfWork = unitOfWork;
            _customerService = customerService;
           
        }
        public async Task<BaseResponse<UserDto>> GetUserByEmail(string email, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if(user == null)
            {
                _logger.LogError("User With Email Not Found");
                return new BaseResponse<UserDto>
                {
                    Message = "User With Email Not Found",
                    Status = false
                };
            }
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? string.Empty;
            if (role == "Customer")
            {
                return new BaseResponse<UserDto>
                {
                    Message = "Customer profile fetched",
                    Status = true,
                    Data = new UserDto
                    {

                        Id = user.Id,
                        Email = user.Email,
                        Roles = roles.Select(r => new RoleDto { Name = r }).ToList(),
                        Customer = new Models.DTOs.Customers.CustomerDto
                        {
                            Id = user.Customer.Id,
                            FirstName = user.Customer.FirstName,
                            FullName = $"{user.Customer.FirstName} {user.Customer.LastName}",
                            Gender = user.Customer.Gender,
                            PhoneNumber = user.Customer.PhoneNumber,
                            DateOfBirth = user.Customer.DateOfBirth,
                            DateCreated = user.Customer.DateCreated,
                            Address = user.Customer.Address,

                        }
                    }
                };
            }

            return new BaseResponse<UserDto>
            {
                Message = "Admin profile fetched",
                Status = true,
                Data = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Roles = role.Select(r => new RoleDto { Name = role }).ToList(),
                    Admin = new AdminDto
                    {
                        FullName = $"{user.Admin.FirstName} {user.Admin.LastName}"

                    }

                }
            };

        }
        public async Task<BaseResponse<UserDto>> GetUserProfileByUserId(Guid userId, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetUserProfile(userId);
            if (user == null)
            {
                return new BaseResponse<UserDto>
                {
                    Message = "User not found",
                    Status = false
                };
            }
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? string.Empty;

            if (role == "Customer")
            {
                return new BaseResponse<UserDto>
                {
                    Message = "Customer profile fetched",
                    Status = true,
                    Data = new UserDto
                    {

                        Id = user.Id,
                        Email = user.Email,
                        Roles = roles.Select(r => new RoleDto { Name = r }).ToList(),
                        Customer = new Models.DTOs.Customers.CustomerDto
                        {
                            Id = user.Customer.Id,
                            FirstName = user.Customer.FirstName,
                            FullName = $"{user.Customer.FirstName} {user.Customer.LastName}",
                            Gender = user.Customer.Gender,
                            PhoneNumber = user.Customer.PhoneNumber,
                            DateOfBirth = user.Customer.DateOfBirth,
                            DateCreated = user.Customer.DateCreated,
                            Address = user.Customer.Address,
                            
                        }
                    }
                };
            }
            
            return new BaseResponse<UserDto>
            {
                Message = "Admin profile fetched",
                Status = true,
                Data = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Roles = role.Select(r => new RoleDto { Name = role }).ToList(),
                    Admin = new AdminDto
                    {
                        FullName = $"{user.Admin.FirstName} {user.Admin.LastName}"
                        
                    }

                }
            };


        }

        public async Task<BaseResponse<LoginResponseModel>> GoogleLoginOrRegisterAsync(GoogleUserDTO googleUser, CancellationToken cancellationToken)
        {
            return await _customerService.GoogleLoginOrRegisterAsync(googleUser, cancellationToken);
        }

        public async Task<BaseResponse<LoginResponseModel>> LoginAsync(LoginRequestModel request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetUserByEmail(request.Email);
            if (user == null)
            {
                _logger.LogError("Invalid credentials");
                return new BaseResponse<LoginResponseModel>
                {
                    Message = "Invalid credentials",
                    Status = false
                };
            }
            var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!isPasswordValid)
            {
                _logger.LogError("Invalid credentials");
                return new BaseResponse<LoginResponseModel>
                {
                    Message = "Invalid credentials",
                    Status = false
                };
            }
            var roles = await _userManager.GetRolesAsync(user);

            var role = roles.FirstOrDefault() ?? string.Empty;

            if (role == "Customer")
            {
                return new BaseResponse<LoginResponseModel>
                {
                    Message = "Login successful",
                    Status = true,
                    Data = new LoginResponseModel
                    {

                        UserId = user.Id,
                        Email = user.Email,
                        Roles = roles.Select(r => new RoleDto { Name = r }).ToList(),
                        FirstName = user.Customer != null ? $"{user.Customer.FirstName}" : string.Empty,
                        FullName = user.Admin != null ? $"{user.Customer?.FullName()}" : string.Empty,




                    }
                };
            }
           
            return new BaseResponse<LoginResponseModel>
            {
                Message = "Login successful",
                Status = true,
                Data = new LoginResponseModel
                {
                    UserId = user.Id,
                    Email = user.Email,
                    Roles = role.Select(r => new RoleDto { Name = role }).ToList(),
                    FirstName = user.Admin != null ? $"{user.Admin.FirstName}" : string.Empty,
                    FullName = user.Admin != null ? $"{user.Admin.FullName()}" : string.Empty,

                }
            };
        }
       
    }
}


