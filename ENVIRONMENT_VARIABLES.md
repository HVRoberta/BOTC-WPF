# Environment Variables Reference

This document provides a complete reference for configuring the BOTC application via environment variables.

## Configuration Precedence

Environment variables take precedence over `appsettings.json` files. Use the `.` separator to represent nested properties.

## ASP.NET Core API Configuration

### Database Connection

**Environment Variable:** `ConnectionStrings__DefaultConnection`

Example (PostgreSQL):
```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=botc;Username=postgres;Password=postgres"
```

Alternative with `Database` section (if using the new structure):
```powershell
$env:Database__ConnectionString = "Host=localhost;Port=5432;Database=botc;Username=postgres;Password=postgres"
```

### Client Configuration

**Environment Variable:** `Client__BaseUrl`

The base URL of the API as seen by clients. Used in responses and client configurations.

```powershell
# Development
$env:Client__BaseUrl = "http://localhost:5000"

# Production
$env:Client__BaseUrl = "https://api.example.com"
```

### CORS Configuration

**Environment Variables:**
- `Cors__PolicyName` - Name of the CORS policy (usually "AllowedOrigins")
- `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, etc. - Each allowed origin

```powershell
# Single origin
$env:Cors__PolicyName = "AllowedOrigins"
$env:Cors__AllowedOrigins__0 = "http://localhost:5001"

# Multiple origins
$env:Cors__AllowedOrigins__0 = "https://app.example.com"
$env:Cors__AllowedOrigins__1 = "https://admin.example.com"
$env:Cors__AllowedOrigins__2 = "https://staging-app.example.com"
```

### SignalR Configuration

**Environment Variable:** `SignalR__RoomLobbyHubPath`

The URL path where the SignalR hub is exposed.

```powershell
# Default
$env:SignalR__RoomLobbyHubPath = "/hubs/room-lobby"

# Custom path
$env:SignalR__RoomLobbyHubPath = "/signalr/rooms/lobby"
```

### Logging Configuration

**Environment Variable:** `Logging__LogLevel__Default`

```powershell
# Development (verbose)
$env:Logging__LogLevel__Default = "Debug"

# Production (minimal)
$env:Logging__LogLevel__Default = "Information"
```

### ASP.NET Core Hosting

**Environment Variable:** `ASPNETCORE_ENVIRONMENT`

Controls which environment-specific configuration file is loaded.

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"  # Loads appsettings.Development.json
$env:ASPNETCORE_ENVIRONMENT = "Production"   # Loads appsettings.Production.json
```

**Environment Variable:** `ASPNETCORE_URLS`

The URLs the server listens on.

```powershell
$env:ASPNETCORE_URLS = "http://localhost:5000"
```

## WPF Desktop Client Configuration

### API Base URL

**Environment Variable:** `Api__BaseUrl`

The base URL of the API server the desktop client connects to.

```powershell
# Development
$env:Api__BaseUrl = "http://localhost:5000"

# Production
$env:Api__BaseUrl = "https://api.example.com"
```

## Example Deployment Scripts

### Development Environment Setup

```powershell
# Clear any existing environment variables
$env:ASPNETCORE_ENVIRONMENT = $null
$env:ConnectionStrings__DefaultConnection = $null

# Set development environment
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=botc;Username=postgres;Password=postgres"
$env:Client__BaseUrl = "http://localhost:5000"
$env:Cors__AllowedOrigins__0 = "http://localhost:5001"
$env:Api__BaseUrl = "http://localhost:5000"

# Verify environment variables are set
Get-Item env:ASPNETCORE_* | Format-Table
Get-Item env:ConnectionStrings__* | Format-Table
Get-Item env:Client__* | Format-Table
Get-Item env:Cors__* | Format-Table
Get-Item env:Api__* | Format-Table
```

### Production Environment Setup

```powershell
# Set production environment
$env:ASPNETCORE_ENVIRONMENT = "Production"

# Database (use secure password storage in production)
$env:ConnectionStrings__DefaultConnection = "Host=prod-db-01.example.com;Port=5432;Database=botc_prod;Username=botc_app;Password=***SECURE_PASSWORD***"

# API URLs
$env:Client__BaseUrl = "https://api.example.com"
$env:Api__BaseUrl = "https://api.example.com"

# CORS Origins (adjust as needed)
$env:Cors__AllowedOrigins__0 = "https://app.example.com"
$env:Cors__AllowedOrigins__1 = "https://admin.example.com"

# Logging
$env:Logging__LogLevel__Default = "Information"
$env:Logging__LogLevel__Microsoft__AspNetCore = "Warning"
```

### Docker Environment (docker-compose.yml reference)

```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Production
  - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=botc;Username=postgres;Password=postgres
  - Client__BaseUrl=https://api.example.com
  - Cors__AllowedOrigins__0=https://app.example.com
  - Logging__LogLevel__Default=Information
```

## Troubleshooting

### Configuration Not Applied

1. **Check environment variable names** - Use `__` (double underscore) for nested properties
2. **Verify case sensitivity** - Environment variable names are case-insensitive on Windows, but follow the convention
3. **Restart application** - Environment variables are read at startup

### Connection String Not Found

```
InvalidOperationException: Database configuration is required. 
Ensure 'Database:ConnectionString' is set.
```

Solution:
```powershell
# Verify the environment variable is set correctly
$env:ConnectionStrings__DefaultConnection
# Or check the configuration file
Get-Content appsettings.json | ConvertFrom-Json | Select-Object -ExpandProperty ConnectionStrings
```

### CORS Not Working

```
Access to XMLHttpRequest at 'https://api.example.com' from origin 
'https://app.example.com' has been blocked by CORS policy
```

Solution:
1. Verify the origin is in `Cors__AllowedOrigins__*`
2. Ensure `WithOrigins()` is configured with exact URLs (including protocol and port)
3. Check that credentials mode matches (if using credentials, ensure `AllowCredentials()` is set)

### SignalR Connection Fails

Check:
1. The `SignalR__RoomLobbyHubPath` matches the server configuration
2. The client uses the correct base URL from `Api__BaseUrl`
3. CORS is properly configured for SignalR websocket connections

## Security Best Practices

1. **Never commit sensitive environment variables** to version control
2. **Use secure password management** for production database credentials
3. **Rotate credentials regularly** especially in production
4. **Use Azure Key Vault**, **AWS Secrets Manager**, or similar for production secrets
5. **Limit CORS origins** to necessary domains only
6. **Log appropriately** - Use `Information` level in production, not `Debug`

## Reference

For complete Microsoft documentation on configuration in ASP.NET Core, see:
https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration

