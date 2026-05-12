using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace EMS.Infrastructure.Storage;

public static class UploadStoragePaths
{
    private const string UploadsRootKey = "Storage:UploadsRoot";

    public static string ResolveUploadsRoot(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var configuredPath = configuration[UploadsRootKey];
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return Path.IsPathRooted(configuredPath)
                ? Path.GetFullPath(configuredPath)
                : Path.GetFullPath(Path.Combine(environment.ContentRootPath, configuredPath));
        }

        return Path.Combine(ResolveWebRoot(environment), "uploads");
    }

    public static string ResolveItemUploadsRoot(IConfiguration configuration, IWebHostEnvironment environment) =>
        Path.Combine(ResolveUploadsRoot(configuration, environment), "items");

    private static string ResolveWebRoot(IWebHostEnvironment environment) =>
        !string.IsNullOrWhiteSpace(environment.WebRootPath)
            ? environment.WebRootPath
            : Path.Combine(AppContext.BaseDirectory, "wwwroot");
}
