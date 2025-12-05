using EMS.Identity;
using EMS.Implementation.Respositories;
using EMS.Implementation.Services;
using EMS.Interfaces.Repositories;
using EMS.Interfaces.Services;
using EMS.Models.Entities;
using FluentValidation;
using FluentValidation.AspNetCore;
using EMS.Persistence.Context;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using EMS.Models.DTOs.Customers.Validation;

var builder = WebApplication.CreateBuilder(args);



// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.Scan(scan => scan
    .FromApplicationDependencies(a => a.FullName!.StartsWith("EMS"))
    .AddClasses(c => c.Where(t => t.Name.EndsWith("Service")))
        .AsImplementedInterfaces()
        .WithScopedLifetime()
    .AddClasses(c => c.Where(t => t.Name.EndsWith("Repository")))
        .AsImplementedInterfaces()
        .WithScopedLifetime());

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddDbContext<EmsContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("EMSContext"),
        new MySqlServerVersion(new Version(9, 0, 0))
    ));

// Register FluentValidation validators and enable MVC integration + client-side adapters
builder.Services.AddValidatorsFromAssemblyContaining<CreateCustomerValidation>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();

builder.Services.AddScoped<IItemRepository, ItemRepository>();
builder.Services.AddScoped<IItemService,ItemService>();
builder.Services.AddScoped<IUserStore<User>, EMS.Identity.UserStore>();
builder.Services.AddScoped<IRoleStore<Role>, RoleStore>();
builder.Services.AddIdentity<User, Role>()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(15);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
}).AddCookie(config =>
{
    config.LoginPath = "/User/Login";
    config.Cookie.Name = "EMS";
    config.LogoutPath = "/User/Logout";
    config.AccessDeniedPath = "/User/Login";
    config.ExpireTimeSpan = TimeSpan.FromMinutes(15);
    config.SlidingExpiration = false;
})
    .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
    {
        options.ClientId = builder.Configuration.GetSection("Google:ClientId").Value;
        options.ClientSecret = builder.Configuration.GetSection("Google:ClientSecret").Value;
        options.CallbackPath = "/signin-google";
        options.SaveTokens = true;

        //options.Scope.Add("profile");
        options.Scope.Add("email");

        //options.ClaimActions.MapJsonKey("picture", "picture");
    })
    ;
builder.Services.AddAuthorization();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=User}/{action=Login}/{id?}")
    .WithStaticAssets();

app.Run();
