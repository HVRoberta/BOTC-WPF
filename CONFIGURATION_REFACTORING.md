# Environment-Based Configuration Refactoring

## Overview

This refactoring introduces a clean, production-ready configuration structure for both the ASP.NET Core API and WPF desktop client. All hardcoded URLs and connection strings have been replaced with strongly-typed options that support environment-specific overrides.

## Architecture Changes

### Configuration Models (New)

The following strongly-typed configuration classes have been created:

#### API Configuration (`BOTC.Presentation.Api/Configuration/`)

- **`DatabaseOptions`** - Database connection configuration
  - `ConnectionString`: PostgreSQL connection string
  
- **`ClientOptions`** - Client base URL configuration
  - `BaseUrl`: Base URL of the API as seen by clients (used in responses, CORS, etc.)
  
- **`CorsOptions`** - CORS policy configuration
  - `PolicyName`: Name of the CORS policy
  - `AllowedOrigins`: Array of allowed origins
  
- **`SignalROptions`** - SignalR hub configuration
  - `RoomLobbyHubPath`: URL path for the Room Lobby SignalR hub

#### Desktop Configuration (`BOTC.Presentation.Desktop/Configuration/`)

- **`ApiOptions`** - API client configuration
  - `BaseUrl`: Base URL of the API server

### Configuration Files

#### ASP.NET Core API

**`appsettings.json`** (base configuration)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "Database": {
    "ConnectionString": ""
  },
  "Client": {
    "BaseUrl": ""
  },
  "Cors": {
    "PolicyName": "AllowedOrigins",
    "AllowedOrigins": []
  },
  "SignalR": {
    "RoomLobbyHubPath": "/hubs/room-lobby"
  }
}
```

**`appsettings.Development.json`** (environment-specific)
- PostgreSQL connection to local development database
- Client base URL points to localhost:5000
- CORS allows localhost:5001 and localhost:3000

**`appsettings.Production.json`** (environment-specific)
- Expects environment variables for sensitive data
- CORS origins configured as needed for production

#### WPF Desktop

**`appsettings.json`** (base configuration)
```json
{
  "Api": {
    "BaseUrl": ""
  }
}
```

**`appsettings.Development.json`** (environment-specific)
- Points to localhost:5000

**`appsettings.Production.json`** (environment-specific)
- Empty, expects configuration from deployment environment

## Key Changes

### Program.cs (API)

1. **Removed hardcoded connection string extraction**
   - Before: Direct extraction of `DefaultConnection` with inline validation
   - After: Uses `IOptions<DatabaseOptions>` pattern

2. **Added CORS configuration**
   - Reads from `Cors` configuration section
   - Applies policy globally with credentials support for real-time connections
   - Only enabled if origins are configured

3. **Added SignalR hub path configuration**
   - Before: Hardcoded `RoomLobbyHubContract.HubRoute`
   - After: Reads from `SignalROptions.RoomLobbyHubPath`

4. **Added startup validation**
   - Validates database configuration exists
   - Validates client base URL exists
   - Provides clear error messages for misconfiguration

### InfrastructureServiceRegistration.cs

1. **Changed method signature**
   - Before: `AddInfrastructure(IServiceCollection services, string connectionString)`
   - After: `AddInfrastructure(IServiceCollection services, IConfiguration configuration)`

2. **Connection string extraction**
   - Now reads from configuration using `GetConnectionString("DefaultConnection")`
   - Provides clear validation error

### DesktopPresentationServiceRegistration.cs

1. **Added configuration registration**
   - Now registers `ApiOptions` with dependency injection
   
2. **Refactored URL resolution**
   - Before: String-based configuration path lookup
   - After: Strongly-typed `ApiOptions` object
   - Falls back to default `http://localhost:5000` if not configured

## Environment Setup

### Development

Set the `ASPNETCORE_ENVIRONMENT` environment variable to `Development`:

**Windows (PowerShell):**
```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
```

**Windows (cmd):**
```cmd
set ASPNETCORE_ENVIRONMENT=Development
```

**Linux/macOS:**
```bash
export ASPNETCORE_ENVIRONMENT=Development
```

### Production

Set the `ASPNETCORE_ENVIRONMENT` environment variable to `Production`:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Production"
```

Then provide required configuration via environment variables:

**API Server:**
```powershell
$env:ConnectionStrings__DefaultConnection = "Host=prod-db;Port=5432;Database=botc;Username=bot;Password=***"
$env:Client__BaseUrl = "https://api.example.com"
$env:Cors__AllowedOrigins__0 = "https://app.example.com"
```

**Desktop Client:**
```powershell
$env:Api__BaseUrl = "https://api.example.com"
```

## Configuration Hierarchy

ASP.NET Core's configuration system loads settings in this order (later values override earlier ones):

1. `appsettings.json`
2. `appsettings.{ASPNETCORE_ENVIRONMENT}.json` (e.g., `appsettings.Development.json`)
3. Environment variables
4. Command-line arguments

This means:
- Base settings go in `appsettings.json`
- Environment-specific overrides go in environment files
- Sensitive data (passwords, secrets) go in environment variables
- Runtime overrides can be passed via command line

## Example Configuration Scenarios

### Local Development

**API Configuration:**
- Database: Local PostgreSQL instance
- Client Base URL: http://localhost:5000
- CORS: Allows localhost:5001 (WPF client), localhost:3000 (alternative clients)

**Desktop Configuration:**
- API Base URL: http://localhost:5000

### Cloud Production

**API Configuration (via environment variables):**
- Database: Cloud-hosted PostgreSQL with credentials
- Client Base URL: https://api.example.com
- CORS: Only allows https://app.example.com

**Desktop Configuration (via environment variables):**
- API Base URL: https://api.example.com

## Migration Guide

If you have custom deployments or configurations:

1. **Update connection strings** from `ConnectionStrings:DefaultConnection` to `Database:ConnectionString`
2. **Update client base URLs** to use `Client:BaseUrl`
3. **Configure CORS origins** in the `Cors:AllowedOrigins` array
4. **Update SignalR hub path** if custom (defaults to `/hubs/room-lobby`)

## Benefits

✅ **No hardcoded values** - All configuration is externalized
✅ **Environment-specific** - Different configs for Dev, Staging, Production
✅ **Strongly-typed** - Compile-time safety via `IOptions<T>`
✅ **Standards-based** - Follows Microsoft configuration conventions
✅ **Secure** - Supports environment variables for sensitive data
✅ **Maintainable** - Clear separation of concerns
✅ **Production-ready** - Suitable for Docker, cloud deployments, CI/CD pipelines

## Testing Configuration

When running tests, ensure your test project:
1. Sets `ASPNETCORE_ENVIRONMENT` to `Development` or creates appropriate `appsettings.Test.json`
2. Provides test database connection string
3. Configures mock origins if CORS validation is tested

