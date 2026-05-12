# Database Transfer

This project now supports a one-time database copy command for moving data from a local MySQL database to an online MySQL database.

## Option 1: Use your configured online connection string

If `ConnectionStrings:EMSContext` already points to your online database, provide only the local source connection:

```powershell
$env:EMS_SOURCE_CONNECTION="Server=localhost;Database=ElectricalManagementSystem;User ID=root;Password=YOUR_LOCAL_PASSWORD;SslMode=None;"
dotnet run -- --transfer-db
```

## Option 2: Pass both source and target directly

```powershell
dotnet run -- --transfer-db `
  --source-connection "Server=localhost;Database=ElectricalManagementSystem;User ID=root;Password=YOUR_LOCAL_PASSWORD;SslMode=None;" `
  --target-connection "Server=YOUR_ONLINE_HOST;Database=YOUR_ONLINE_DATABASE;User ID=YOUR_ONLINE_USER;Password=YOUR_ONLINE_PASSWORD;SslMode=Required;"
```

## Optional configuration key

If you prefer config over environment variables, you can add `ConnectionStrings:EMSLocalSource` and then run:

```powershell
dotnet run -- --transfer-db
```

## What the command does

- Applies pending EF Core migrations to the target database
- Clears the target application tables
- Copies all EMS data from source to target while preserving IDs and relationships

## Important note

The database transfer does not copy files from `wwwroot/uploads`. If your items use uploaded images, make sure those files are also present on the online server.
