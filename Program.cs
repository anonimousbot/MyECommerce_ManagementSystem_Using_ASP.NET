using EMS.Identity;
using EMS.Implementation.Respositories;
using EMS.Implementation.Services;
using EMS.Infrastructure.DataTransfer;
using EMS.Infrastructure.HealthChecks;
using EMS.Infrastructure.Storage;
using EMS.Interfaces.Repositories;
using EMS.Interfaces.Services;
using EMS.Models.Entities;
using EMS.Models.Security;
using FluentValidation;
using FluentValidation.AspNetCore;
using EMS.Persistence.Context;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EMS.Models.DTOs.Customers.Validation;
using EMS.Models.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using System.Text.Json;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

var renderPort = Environment.GetEnvironmentVariable("PORT");
if (int.TryParse(renderPort, out var port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}



// Add services to the container.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

if (DatabaseTransferRunner.ShouldRun(args))
{
    using var transferLoggerFactory = LoggerFactory.Create(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.AddDebug();
    });

    var transferLogger = transferLoggerFactory.CreateLogger("DatabaseTransfer");
    await DatabaseTransferRunner.RunAsync(args, builder.Configuration, transferLogger);
    return;
}

builder.Services.AddProblemDetails();
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

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
    options.UseMySql(
        builder.Configuration.GetConnectionString("EMSContext"),
        new MySqlServerVersion(new Version(9, 0, 0)),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure()));

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Register FluentValidation validators and enable MVC integration + client-side adapters
builder.Services.AddValidatorsFromAssemblyContaining<CreateCustomerValidation>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();

// Note: IItemRepository and IItemService are already registered by Scrutor scan above.
// Explicit re-registration removed to avoid duplicate DI entries.
builder.Services.AddScoped<IUserStore<User>, EMS.Identity.UserStore>();
builder.Services.AddScoped<IRoleStore<Role>, RoleStore>();
builder.Services.AddIdentity<User, Role>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = true;
    })
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

builder.Services.AddOptions<PaystackOptions>()
    .Bind(builder.Configuration.GetSection("Paystack"))
    .Validate(options => !string.IsNullOrWhiteSpace(options.SecretKey), "Paystack:SecretKey must be configured.")
    .ValidateOnStart();

builder.Services.AddHttpClient<IPaystackClient, PaystackClient>(client =>
{
    client.BaseAddress = new Uri("https://api.paystack.co/");
});

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database");

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "global",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});
builder.Services.ConfigureApplicationCookie(config =>
{
    config.LoginPath = "/User/Login";
    config.LogoutPath = "/User/Logout";
    config.AccessDeniedPath = "/Account/AccessDenied";
    config.Cookie.Name = "EMS";
    config.Cookie.HttpOnly = true;
    config.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    config.Cookie.SameSite = SameSiteMode.Strict;
    config.ExpireTimeSpan = TimeSpan.FromHours(2);
    config.SlidingExpiration = true; // extends on activity
});

builder.Services.AddAuthentication()
    .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
    {
        options.ClientId = builder.Configuration.GetSection("Google:ClientId").Value;
        options.ClientSecret = builder.Configuration.GetSection("Google:ClientSecret").Value;
        options.CallbackPath = "/signin-google";
        options.SaveTokens = true;
        options.Scope.Add("email");
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.AdminOnly, policy => policy.RequireRole("Admin"));
    options.AddPolicy(AuthorizationPolicies.CustomerOnly, policy => policy.RequireRole("Customer"));
});
var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
        .CreateLogger("Startup");

    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<EmsContext>();
        await dbContext.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migration failed during startup.");
        throw;
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

var uploadsRoot = UploadStoragePaths.ResolveUploadsRoot(app.Configuration, app.Environment);
Directory.CreateDirectory(uploadsRoot);

var defaultUploadsRoot = Path.Combine(app.Environment.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot"), "uploads");
if (!string.Equals(
        Path.GetFullPath(defaultUploadsRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
        Path.GetFullPath(uploadsRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
        StringComparison.OrdinalIgnoreCase))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(uploadsRoot),
        RequestPath = "/uploads"
    });
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseRateLimiter();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description
            })
        }));
    }
});

app.Run();
