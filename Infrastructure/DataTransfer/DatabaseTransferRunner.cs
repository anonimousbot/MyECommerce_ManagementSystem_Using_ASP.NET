using EMS.Models.Entities;
using EMS.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EMS.Infrastructure.DataTransfer;

public static class DatabaseTransferRunner
{
    private const string TransferCommand = "--transfer-db";
    private const string SourceArgument = "--source-connection";
    private const string TargetArgument = "--target-connection";
    private const string SourceConnectionName = "EMSLocalSource";
    private const string TargetConnectionName = "EMSContext";
    private const string SourceEnvironmentVariable = "EMS_SOURCE_CONNECTION";
    private const string TargetEnvironmentVariable = "EMS_TARGET_CONNECTION";

    public static bool ShouldRun(string[] args) =>
        args.Any(arg => string.Equals(arg, TransferCommand, StringComparison.OrdinalIgnoreCase));

    public static async Task RunAsync(
        string[] args,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var sourceConnection = ResolveConnectionString(
            args,
            configuration,
            SourceArgument,
            SourceConnectionName,
            SourceEnvironmentVariable);

        var targetConnection = ResolveConnectionString(
            args,
            configuration,
            TargetArgument,
            TargetConnectionName,
            TargetEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(sourceConnection))
        {
            throw new InvalidOperationException(
                $"No source connection string was found. Use {SourceArgument}, ConnectionStrings:{SourceConnectionName}, or {SourceEnvironmentVariable}.");
        }

        if (string.IsNullOrWhiteSpace(targetConnection))
        {
            throw new InvalidOperationException(
                $"No target connection string was found. Use {TargetArgument}, ConnectionStrings:{TargetConnectionName}, or {TargetEnvironmentVariable}.");
        }

        if (string.Equals(
                sourceConnection.Trim(),
                targetConnection.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Source and target connection strings are identical. Aborting transfer to avoid copying onto the same database.");
        }

        logger.LogInformation("Starting database transfer from source to target.");

        await using var sourceContext = CreateContext(sourceConnection);
        await using var targetContext = CreateContext(targetConnection);

        if (!await sourceContext.Database.CanConnectAsync(cancellationToken))
        {
            throw new InvalidOperationException("Could not connect to the source database.");
        }

        await targetContext.Database.MigrateAsync(cancellationToken);

        await using var transaction = await targetContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await targetContext.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 0;", cancellationToken);

            await ClearTargetAsync(targetContext, cancellationToken);

            await CopyTableAsync<Role>(sourceContext, targetContext, logger, cancellationToken);
            await CopyTableAsync<User>(sourceContext, targetContext, logger, cancellationToken);
            await CopyTableAsync<UserRole>(sourceContext, targetContext, logger, cancellationToken);
            await CopyTableAsync<Admin>(sourceContext, targetContext, logger, cancellationToken);
            await CopyTableAsync<Item>(sourceContext, targetContext, logger, cancellationToken);
            await CopyTableAsync<Customer>(sourceContext, targetContext, logger, cancellationToken);
            await CopyTableAsync<Cart>(sourceContext, targetContext, logger, cancellationToken);
            await CopyTableAsync<Order>(sourceContext, targetContext, logger, cancellationToken);
            await CopyTableAsync<OrderItem>(sourceContext, targetContext, logger, cancellationToken);
            await CopyTableAsync<CartItem>(sourceContext, targetContext, logger, cancellationToken);
            await CopyTableAsync<Payment>(sourceContext, targetContext, logger, cancellationToken);

            await targetContext.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 1;", cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation("Database transfer completed successfully.");
        }
        catch
        {
            await targetContext.Database.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 1;", cancellationToken);
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static EmsContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<EmsContext>()
            .UseMySql(
                connectionString,
                new MySqlServerVersion(new Version(9, 0, 0)),
                mySqlOptions => mySqlOptions.EnableRetryOnFailure())
            .Options;

        return new EmsContext(options);
    }

    private static async Task ClearTargetAsync(EmsContext context, CancellationToken cancellationToken)
    {
        await context.Set<Payment>().ExecuteDeleteAsync(cancellationToken);
        await context.Set<CartItem>().ExecuteDeleteAsync(cancellationToken);
        await context.Set<OrderItem>().ExecuteDeleteAsync(cancellationToken);
        await context.Set<Order>().ExecuteDeleteAsync(cancellationToken);
        await context.Set<Cart>().ExecuteDeleteAsync(cancellationToken);
        await context.Set<Customer>().ExecuteDeleteAsync(cancellationToken);
        await context.Set<Admin>().ExecuteDeleteAsync(cancellationToken);
        await context.Set<UserRole>().ExecuteDeleteAsync(cancellationToken);
        await context.Set<Item>().ExecuteDeleteAsync(cancellationToken);
        await context.Set<User>().ExecuteDeleteAsync(cancellationToken);
        await context.Set<Role>().ExecuteDeleteAsync(cancellationToken);
    }

    private static async Task CopyTableAsync<TEntity>(
        EmsContext sourceContext,
        EmsContext targetContext,
        ILogger logger,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var rows = await sourceContext.Set<TEntity>()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            logger.LogInformation("Skipped {EntityName}: no rows found.", typeof(TEntity).Name);
            return;
        }

        await targetContext.Set<TEntity>().AddRangeAsync(rows, cancellationToken);
        await targetContext.SaveChangesAsync(cancellationToken);
        targetContext.ChangeTracker.Clear();

        logger.LogInformation("Copied {Count} {EntityName} rows.", rows.Count, typeof(TEntity).Name);
    }

    private static string? ResolveConnectionString(
        string[] args,
        IConfiguration configuration,
        string argumentName,
        string connectionStringName,
        string environmentVariableName)
    {
        var fromArgument = ReadArgumentValue(args, argumentName);
        if (!string.IsNullOrWhiteSpace(fromArgument))
        {
            return fromArgument;
        }

        var fromConfiguration = configuration.GetConnectionString(connectionStringName);
        if (!string.IsNullOrWhiteSpace(fromConfiguration))
        {
            return fromConfiguration;
        }

        return Environment.GetEnvironmentVariable(environmentVariableName);
    }

    private static string? ReadArgumentValue(string[] args, string argumentName)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], argumentName, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return null;
    }
}
