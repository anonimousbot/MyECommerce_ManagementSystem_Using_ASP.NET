using EMS.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EMS.Persistence.Context
{
    public class EmsContext : DbContext
    {
        public EmsContext(DbContextOptions<EmsContext> options) : base(options)
        {


        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Admin>()
            .HasOne(a => a.User)
            .WithOne(u => u.Admin)
            .HasForeignKey<Admin>(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Admin>()
     .Property(a => a.Gender)
     .HasConversion<byte>();
            SeedAdminData(builder);

            SeedRoleData(builder);


            builder.Entity<Customer>()
                .HasOne(c => c.User)
                .WithOne(u => u.Customer)
                .HasForeignKey<Customer>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            //builder.Entity<User>()
            //.HasOne(u => u.Customer)
            //.WithOne(p => p.User)
            //.HasForeignKey<Customer>(p => p.UserId)
            //.OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserRole>()
             .HasOne(ur => ur.User)
             .WithMany(u => u.UserRoles)
             .HasForeignKey(ur => ur.UserId);

            builder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);

            builder.Entity<User>()
           .HasIndex(u => u.Email)
           .IsUnique();

            builder.Entity<Customer>()
                .HasMany(o => o.Orders)
                .WithOne(c => c.Customer)
                .HasForeignKey(c => c.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Customer>()
                .HasOne(c => c.Cart)
                .WithOne(c => c.Customer)
                .HasForeignKey<Cart>(c => c.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CartItem>()
                .HasOne(ci => ci.Cart)
                .WithMany(c => c.Items)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CartItem>()
                .HasOne(ci => ci.Item)
                .WithMany()
                .HasForeignKey(ci => ci.ItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CartItem>()
                .HasIndex(ci => new { ci.CartId, ci.ItemId })
                .IsUnique();

            builder.Entity<OrderItem>()
                .HasOne(o => o.Order)
                .WithMany(o => o.OrderItem)
                .HasForeignKey(o => o.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<OrderItem>()
                .HasOne(i => i.Item)
                .WithMany(o => o.OrderItem)
                .HasForeignKey(i => i.ItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Order>()
                .HasIndex(o => o.PaymentReference)
                .IsUnique();

            builder.Entity<Payment>()
                .HasOne(p => p.Order)
                .WithOne()
                .HasForeignKey<Payment>(p => p.OrderId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Payment>()
                .HasOne(p => p.Customer)
                .WithMany()
                .HasForeignKey(p => p.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Payment>()
                .HasIndex(p => p.Reference)
                .IsUnique();

            base.OnModelCreating(builder);

        }
        private void SeedAdminData(ModelBuilder modelBuilder)
        {

            var adminRoleId = new Guid("d2719e67-52f4-4f9c-bdb2-123456789abc");
            var adminUserId = new Guid("c8f2e5ab-9f34-4b97-8b7c-1a5e86c77e42");

            var role = new Role
            {
                Id = adminRoleId,
                Name = "Admin",
                Description = "Has full permissions",
                DateCreated = DateTime.SpecifyKind(new DateTime(2025, 11, 10), DateTimeKind.Utc)
            };

            var hasher = new PasswordHasher<object>();
            var passwordHash = hasher.HashPassword(null, "Admin@001");
            var adminUser = new User
            {
                Id = adminUserId,
                Email = "Admin001@gmail.com",
                PasswordHash = "AQAAAAIAAYagAAAAEJjieFsJGM2Xgr+WpuS3juOABbBCvbqSvpym4WzP/SDMuvGz6qH+EFgm19l8SUHUGA==",
                EmailConfirmed = true,
                DateCreated = DateTime.SpecifyKind(new DateTime(2025, 11, 10), DateTimeKind.Utc),
            };


            var userRole = new UserRole
            {
                Id = new Guid("7ad9b1e1-4c23-46a2-b8e4-219ab417f71f"),
                RoleId = adminRoleId,
                UserId = adminUserId,
                DateCreated = DateTime.SpecifyKind(new DateTime(2025, 11, 10), DateTimeKind.Utc)
            };

            var adminProfile = new Admin
            {
                Id = new Guid("f0e25b73-7d1a-4c19-8b2f-09a3efb40d12"),
                FirstName = "Admin",
                LastName = "Hms",
                Address = "Lagos State",
                Gender = Models.Enums.Gender.Male,
                PhoneNumber = "+2349020880996",
                DateOfBirth = DateTime.SpecifyKind(new DateTime(1997, 11, 10), DateTimeKind.Utc),
                UserId = adminUserId,
                DateCreated = DateTime.SpecifyKind(new DateTime(2025, 11, 10), DateTimeKind.Utc),
            };

            modelBuilder.Entity<Role>().HasData(role);
            modelBuilder.Entity<Admin>().HasData(adminProfile);
            modelBuilder.Entity<User>().HasData(adminUser);
            modelBuilder.Entity<UserRole>().HasData(userRole);
        }
        private void SeedRoleData(ModelBuilder modelBuilder)
        {
            var roles = new List<Role>
            {
                new Role
                {
                    Id = new Guid("67cb1ae1-91ab-480f-a236-d7f676f7ab99"),
                    Name = "Customer",
                    Description = "Can Order Items from the Store",
                    DateCreated = new DateTime(2026, 4, 1, 13, 15, 59, 214, DateTimeKind.Utc).AddTicks(1295),
                },
              
            };

            modelBuilder.Entity<Role>().HasData(roles);
        }
        DbSet<Admin> Admins => Set<Admin>();
        DbSet<Customer> Customers => Set<Customer>();
        DbSet<User> Users => Set<User>();
        DbSet<UserRole> UserRoles => Set<UserRole>();
        DbSet<Item> Items => Set<Item>();
        DbSet<Order> Orders => Set<Order>();
        DbSet<OrderItem> OrderItems => Set<OrderItem>();
        DbSet<Role> Roles => Set<Role>();
        DbSet<Cart> Carts => Set<Cart>();
        DbSet<CartItem> CartItems => Set<CartItem>();
        DbSet<Payment> Payments => Set<Payment>();

    }
}
