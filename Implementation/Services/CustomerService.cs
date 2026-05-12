using System.Reflection;
using System.Transactions;
using EMS.Contracts.Services;
using EMS.Interfaces.Repositories;
using EMS.Interfaces.Services;
using EMS.Models.DTOs;
using EMS.Models.DTOs.Customers;
using EMS.Models.DTOs.Users;
using EMS.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace EMS.Implementation.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly IUserRepository _userRepository;
        private readonly UserManager<User> _userManager;
        private readonly IIdentityService _identityService;
        private readonly IRoleRepository _role_repository;
        private readonly ICustomerRepository _customer_repository;
        private readonly IUnitOfWork _unitOfWork;
        ILogger<CustomerService> _logger;
        public CustomerService(IUserRepository userRepository, UserManager<User> userManager,
            IIdentityService identityService, IRoleRepository roleRepository,
            ICustomerRepository customerRepository, IUnitOfWork unitOfWork, ILogger<CustomerService> logger)
        {
            _userRepository = userRepository;
            _userManager = userManager;
            _identityService = identityService;
            _role_repository = roleRepository;
            _customer_repository = customerRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<BaseResponse<bool>> CreateAsync(CreateCustomerRequestModel request)
        {
           var customerExists = await _userRepository.Any(x => x.Email == request.Email);
            if (customerExists)
            {
                _logger.LogError("Customer with email already exist");
                return new BaseResponse<bool>
                {
                    Message = "Customer with email already exist",
                    Status = false
                };
            }

            if (request.PasswordHash != request.ConfirmPassword)
                return new BaseResponse<bool>
                {
                    Message = "Password doesn't match!",
                    Status = false,
                };

            (var passwordResult, var message) = ValidatePassword(request.PasswordHash);
            if (!passwordResult) return new BaseResponse<bool> { Message = message, Status = false };

            // Revert to the previously working approach: minimal User object, use identity service to set PasswordHash
            var customerUser = new User
            {
                Email = request.Email,
                DateCreated = DateTime.UtcNow,
            };

            // Use your identity service to create the hash (as before)
            customerUser.PasswordHash = _identityService.GetPasswordHash(request.PasswordHash);

            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Create user (previous working call)
                var createResult = await _userManager.CreateAsync(customerUser);
                if (createResult == null || !createResult.Succeeded)
                {
                    var errors = createResult == null
                        ? "Unknown error creating user"
                        : string.Join(" | ", createResult.Errors.Select(e => e.Description));
                    _logger.LogError("User creation unsuccessful: {Errors}", errors);

                    await transaction.RollbackAsync();
                    return new BaseResponse<bool>
                    {
                        Message = "User creation unsuccessful: " + errors,
                        Status = false,
                    };
                }

                // Add to Customer role
                var addRoleResult = await _userManager.AddToRoleAsync(customerUser, "Customer");
                if (!addRoleResult.Succeeded)
                {
                    var errors = string.Join(" | ", addRoleResult.Errors.Select(e => e.Description));
                    _logger.LogError("Unable to add user to role: {Errors}", errors);
                    await transaction.RollbackAsync();
                    return new BaseResponse<bool>
                    {
                        Message = "Unable to add user to role: " + errors,
                        Status = false
                    };
                }

                var customer = new Customer
                {
                    UserId = customerUser.Id,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Address = request.Address,
                    Gender = request.Gender,
                    DateOfBirth = request.DateOfBirth,
                    PhoneNumber = request.PhoneNumber,
                    DateCreated = DateTime.UtcNow,
                };
                customer.FullName();

                var createCustomer = await _customer_repository.Add(customer);
                await _unitOfWork.SaveChangesAsync(CancellationToken.None);

                if (createCustomer == null)
                {
                    _logger.LogError("Customer Couldn't be Added");
                    await transaction.RollbackAsync();
                    return new BaseResponse<bool>
                    {
                        Message = "Customer Couldn't be Added",
                        Status = false
                    };
                }

                await transaction.CommitAsync();

                _logger.LogInformation("Customer Added Successfully");
                return new BaseResponse<bool>
                {
                    Message = "Customer Added Successfully",
                    Status = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer, rolling back.....");
                try { await transaction.RollbackAsync(); } catch (Exception rbEx) { _logger.LogError(rbEx, "Rollback failed"); }
                return new BaseResponse<bool>
                {
                    Message = "An error occurred while creating customer",
                    Status = false
                };
            }
        }

        public async Task<BaseResponse<bool>> DeleteAsync(Guid id)
        {
            var customer = await _customer_repository.CheckCustomerWithUser(id);
            if (customer == null)
            {
                _logger.LogError("Customer user not found");
                return new BaseResponse<bool>
                {
                    Message = $"{customer?.Id} not found",
                    Status = false
                };
            }

            var customerGet = await _customer_repository.Get<Customer>(x => x.Id == id);
            if (customerGet == null)
            {
                _logger.LogError("Customer not found");
                return new BaseResponse<bool>
                {
                    Message = $"{customerGet?.Id} not found",
                    Status = false
                };
            }
            var users = await _userManager.FindByIdAsync(customer.UserId.ToString());
            if (users == null)
            {
                _logger.LogError("Customer Not Found In User Table");
                return new BaseResponse<bool>
                {
                    Message = "Customer Not Found In User Table",
                    Status = false
                };
            }
            _customer_repository.Delete(customerGet);
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);
            var deleteUser = _userManager.DeleteAsync(users);
            if (deleteUser == null)
            {
                _logger.LogError("Customer Could Not be deleted from usertable");
                return new BaseResponse<bool>
                {
                    Message = "Customer Could Not be deleted from Usertable",
                    Status = false
                };
            }
            
            _logger.LogInformation($"Delete {customerGet.Id}");
            return new BaseResponse<bool>
            {
                Message = "Customer deleted successfully",
                Status = true
            };

        }

        public Task<IReadOnlyList<CustomerDto>> GetAsync(string param, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<BaseResponse<CustomerDto>> GetByIdAsync(Guid customerId, CancellationToken cancellationToken)
        {
            var getCustomer = await _customer_repository.GetCustomersByIdAsync(customerId);
            if (getCustomer == null)
            {
                _logger.LogError("Customer doesn't exist");
                return new BaseResponse<CustomerDto>
                {
                    Message = "Customer doesn't exist",
                    Status = false
                };
            }
            _logger.LogInformation("Customer fetched successfully");
            return new BaseResponse<CustomerDto>
            {
                Message = "Customer fetched successfully",
                Status = true,
                Data = new CustomerDto
                {
                    Id = getCustomer.Id,
                    FirstName = getCustomer.FirstName,
                    LastName = getCustomer.LastName,
                    Address = getCustomer.Address,
                    DateOfBirth = getCustomer.DateOfBirth,
                    Gender = getCustomer.Gender,
                    PhoneNumber = getCustomer.PhoneNumber,
                    User = getCustomer.User,
                    DateCreated = getCustomer.DateCreated,
                }
            };
        }

        public async Task<BaseResponse<CustomerDto>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            var customer = await _customer_repository.Get<Customer>(c => c.UserId == userId);
            if (customer == null)
            {
                _logger.LogError("Customer doesn't exist for user {UserId}", userId);
                return new BaseResponse<CustomerDto>
                {
                    Message = "Customer doesn't exist",
                    Status = false
                };
            }

            return await GetByIdAsync(customer.Id, cancellationToken);
        }

        public async Task<BaseResponse<IReadOnlyList<CustomerDto>>> GetCustomerAsync(CancellationToken cancellationToken, int pageNumber = 1, int pageSize = 10)
        {
            var (normalizedPageNumber, normalizedPageSize) = PaginationHelper.Normalize(pageNumber, pageSize);
            var query = _customer_repository.Query<Customer>().AsNoTracking();
            var totalItems = await query.CountAsync(cancellationToken);
            var pagedCustomers = await query
                .OrderByDescending(c => c.DateCreated)
                .Select(c => new CustomerDto
                {
                    Id = c.Id,
                    Address = c.Address,
                    Gender = c.Gender,
                    PhoneNumber = c.PhoneNumber,
                    DateModified = c.DateModified,
                    DateCreated = c.DateCreated,
                    DateOfBirth = c.DateOfBirth,
                    FullName = (c.FirstName ?? string.Empty) + " " + (c.LastName ?? string.Empty)
                })
                .Skip((normalizedPageNumber - 1) * normalizedPageSize)
                .Take(normalizedPageSize)
                .ToListAsync(cancellationToken);

            if (totalItems == 0)
            {
                _logger.LogError("No data found");
                return new BaseResponse<IReadOnlyList<CustomerDto>>()
                {
                    Message = "No data found",
                    Status = false,
                    Data = [],
                    Pagination = PaginationHelper.Create(normalizedPageNumber, normalizedPageSize, totalItems)
                };
            }

            return new BaseResponse<IReadOnlyList<CustomerDto>>
            {
                Message = "Data Fetched Succcessfully",
                Status = true,
                Data = pagedCustomers,
                Pagination = PaginationHelper.Create(normalizedPageNumber, normalizedPageSize, totalItems)
            };
        }

        public async Task<BaseResponse<LoginResponseModel>> GoogleLoginOrRegisterAsync(GoogleUserDTO googleUser, CancellationToken cancellationToken)
        {
            try
            {
                var existingUser = await _userRepository.GetUserByGoogleId(googleUser.GoogleId);
                if (existingUser != null)
                {
                    var roles = await _userManager.GetRolesAsync(existingUser);
                    roles = await EnsureUserHasRoleAsync(existingUser, roles);
                    var (firstName, fullName) = ResolveNames(existingUser, googleUser.FullName);
                    _logger.LogInformation("Google user logged in succesfully");
                    return new BaseResponse<LoginResponseModel>
                    {
                        Message = "Login successful",
                        Status = true,
                        Data = new LoginResponseModel
                        {
                            UserId = existingUser.Id,
                            Email = existingUser.Email,
                            Roles = roles.Select(r => new Models.DTOs.Roles.RoleDto { Name = r }).ToList(),
                            FirstName = firstName,
                            FullName = fullName,
                        }
                    };
                }
                var userByEmail = await _userRepository.Get<User>(u => u.Email == googleUser.Email);
                if (userByEmail != null)
                {
                    userByEmail.GoogleId = googleUser.GoogleId;
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    var roles = await _userManager.GetRolesAsync(userByEmail);
                    roles = await EnsureUserHasRoleAsync(userByEmail, roles);
                    var (firstName, fullName) = ResolveNames(userByEmail, googleUser.FullName);
                    _logger.LogInformation("Google Account Linked To Existing User");
                    return new BaseResponse<LoginResponseModel>
                    {
                        Message = "Account Linked Succesfully",
                        Status = true,
                        Data = new LoginResponseModel
                        {
                            UserId = userByEmail.Id,
                            Email = userByEmail.Email,
                            Roles = roles.Select(r => new Models.DTOs.Roles.RoleDto { Name = r }).ToList(),
                            FirstName = firstName,
                            FullName = fullName
                        }
                    };
                }
                using var transaction = await _unitOfWork.BeginTransactionAsync();
                try
                {
                    var nameParts = googleUser.FullName?.Split(' ', 2) ?? new[] { "", "" };
                    var firstName = nameParts.Length > 0 ? nameParts[0] : "User";
                    var lastName = nameParts.Length > 1 ? nameParts[1] : "";

                    var newUser = new User
                    {
                        Email = googleUser.Email,
                        EmailConfirmed = true,
                        GoogleId = googleUser.GoogleId,
                        PasswordHash = null,
                        DateCreated = DateTime.UtcNow,
                    };
                    var createResult = await _userManager.CreateAsync(newUser);
                    if(!createResult.Succeeded)
                    {
                        var errors = string.Join(" | ", createResult.Errors.Select(e => e.Description));
                        _logger.LogError("User creation failed: {Errors}", errors);
                        await transaction.RollbackAsync();
                        return new BaseResponse<LoginResponseModel>
                        {
                            Message = "User creation failed: " + errors,
                            Status = false
                        };
                    }
                    var addRoleResult = await _userManager.AddToRoleAsync(newUser, "Customer");
                    if(!addRoleResult.Succeeded)
                    {
                        var errors = string.Join(" | ", addRoleResult.Errors.Select(e => e.Description));
                        _logger.LogError("Failed to add user to customer role : {Errors}", errors);
                        await transaction.RollbackAsync();
                        return new BaseResponse<LoginResponseModel>
                        {
                            Message = "Failed to assign customer role: " + errors,
                            Status = false
                        };

                    }
                    var customer = new Customer
                    {
                        UserId = newUser.Id,
                        FirstName = firstName,
                        LastName = lastName,
                        PhoneNumber = "",
                        Address = "",
                        Gender = null,
                        DateOfBirth = null,
                        DateCreated = DateTime.UtcNow
                    };
                    var createCustomer = await _customer_repository.Add(customer);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    if(createCustomer == null)
                    {
                        _logger.LogError("Customer profile creation failed");
                        await transaction.RollbackAsync();
                        return new BaseResponse<LoginResponseModel>
                        {
                            Message = "Failed to create customer profile",
                            Status = false

                        };
                    }
                    await transaction.CommitAsync();
                    var newUserRoles = await _userManager.GetRolesAsync(newUser);
                    _logger.LogInformation("new customer created vis googleOauth");
                    return new BaseResponse<LoginResponseModel>
                    {
                        Message = "Account Created Successfully",
                        Status = true,
                        Data = new LoginResponseModel
                        {
                            UserId = newUser.Id,
                            Email = newUser.Email,
                            Roles = newUserRoles.Select(r => new Models.DTOs.Roles.RoleDto { Name = r }).ToList(),
                            FirstName = firstName,
                            FullName = googleUser.FullName
                        }
                    };
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "Error during google oauth registration");
                    try { await transaction.RollbackAsync(); }
                    catch (Exception rbEx) { _logger.LogError(rbEx, "Rollback failed"); }
                    return new BaseResponse<LoginResponseModel>
                    {
                        Message = "An Error Ocurred during registration",
                        Status = false,
                    };
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error in GoogleLoginOrReisterAsync");
                return new BaseResponse<LoginResponseModel>
                {
                    Message = "An error occurred during google authentication",
                    Status = false,
                };
            }
        }

        private async Task<IList<string>> EnsureUserHasRoleAsync(User user, IList<string> roles)
        {
            if (roles.Count > 0)
            {
                return roles;
            }

            var fallbackRole = user.Admin != null ? "Admin" : user.Customer != null ? "Customer" : null;
            if (fallbackRole == null)
            {
                return roles;
            }

            var addRoleResult = await _userManager.AddToRoleAsync(user, fallbackRole);
            if (!addRoleResult.Succeeded)
            {
                var errors = string.Join(" | ", addRoleResult.Errors.Select(e => e.Description));
                _logger.LogError("Failed to assign fallback role {Role} for Google login user {UserId}: {Errors}", fallbackRole, user.Id, errors);
                return roles;
            }

            return await _userManager.GetRolesAsync(user);
        }

        private static (string firstName, string fullName) ResolveNames(User user, string? fallbackFullName)
        {
            if (user.Customer != null)
            {
                return (user.Customer.FirstName ?? string.Empty, user.Customer.FullName() ?? string.Empty);
            }

            if (user.Admin != null)
            {
                return (user.Admin.FirstName ?? string.Empty, user.Admin.FullName() ?? string.Empty);
            }

            var name = fallbackFullName ?? string.Empty;
            var first = string.IsNullOrWhiteSpace(name)
                ? "User"
                : name.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "User";

            return (first, name);
        }

        public async Task<BaseResponse<bool>> CreateCustomerByGmail(Customer createCustomer)
        {
            var newCustomer = await _customer_repository.Add(createCustomer);
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);

            if (createCustomer == null)
            {
                _logger.LogError("Customer Couldn't be Added");
                return new BaseResponse<bool>
                {
                    Message = "Customer Couldn't be Added",
                    Status = false
                };
            }
            _logger.LogInformation("Customer Added Successfully");
            return new BaseResponse<bool>
            {
                Message = "Customer Added Successfully",
                Status = true
            };

        }
        public async Task<BaseResponse<bool>> UpdateAsync(UpdateCustomerRequestModel model, Guid id)
        {
            var getCustomer = await _customer_repository.GetCustomersByIdAsync(id);
            if (getCustomer == null)
            {
                _logger.LogError("Customer not found");
                return new BaseResponse<bool>
                {
                    Message = "Customer not found",
                    Status = false,
                };
            }
            getCustomer.Address = model.Address;
            getCustomer.PhoneNumber = model.PhoneNumber;
            getCustomer.DateModified = DateTime.UtcNow;
            getCustomer.FirstName = model.FirstName;
            getCustomer.LastName = model.LastName;
            getCustomer.Gender = model.Gender;

            _customer_repository.Update<Customer>(getCustomer);
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);
            return new BaseResponse<bool>
            {
                Message = "Profile Edited Successfully",
                Status = true,
            };

        }

        private static (bool, string?) ValidatePassword(string password)
        {
            // Minimum length of password
            int minLength = 8;

            // Maximum length of password
            int maxLength = 50;

            // Check for null or empty password
            if (string.IsNullOrEmpty(password))
            {
                return (false, "Password cannot be null or empty.");
            }

            // Check length of password
            if (password.Length < minLength || password.Length > maxLength)
            {
                return (false, $"Password must be between {minLength} and {maxLength} characters long.");
            }

            // Check for at least one uppercase letter, one lowercase letter, and one digit
            bool hasUppercase = false;
            bool hasLowercase = false;
            bool hasDigit = false;

            foreach (char c in password)
            {
                if (char.IsUpper(c))
                {
                    hasUppercase = true;
                }
                else if (char.IsLower(c))
                {
                    hasLowercase = true;
                }
                else if (char.IsDigit(c))
                {
                    hasDigit = true;
                }
            }

            if (!hasUppercase || !hasLowercase || !hasDigit)
            {
                return (false, "Password must contain at least one uppercase letter, one lowercase letter, and one digit.");
            }

            // Check for any characters
            string invalidCharacters = @" !""#$%&'()*+,-./:;<=>?@[\\]^_`{|}~";
            if (password.IndexOfAny(invalidCharacters.ToCharArray()) == -1)
            {
                return (false, "Password must contain one or more characters.");
            }

            // Password is valid
            return (true, null);
        }

       
    }
}
