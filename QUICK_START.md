# Quick Start Guide - Configuration Refactoring

## Overview

The BOTC application has been refactored to use environment-based configuration instead of hardcoded values. This guide helps you get started quickly.

## 5-Minute Setup

### For Local Development (Windows)

```powershell
# 1. Set environment to Development
$env:ASPNETCORE_ENVIRONMENT = "Development"

# 2. Ensure PostgreSQL is running on localhost:5432
# - Database: botc
# - Username: postgres  
# - Password: postgres

# 3. Run the API
cd src/BOTC.Presentation.Api
dotnet run

# 4. Run the Desktop Client
cd src/BOTC.Presentation.Desktop
dotnet run
```

That's it! Both applications automatically use the Development configuration.

---

## Configuration Files Overview

```
🏠 Root (appsettings.json)
├─ 📋 Database.ConnectionString (empty)
├─ 🌐 Client.BaseUrl (empty)
├─ 🔐 Cors.AllowedOrigins (empty array)
└─ 📡 SignalR.RoomLobbyHubPath (/hubs/room-lobby)

🔧 Development (appsettings.Development.json)
├─ Database.ConnectionString = Host=localhost;...
├─ Client.BaseUrl = http://localhost:5000
├─ Cors.AllowedOrigins = [http://localhost:5001, ...]
└─ Logging = Debug level

🚀 Production (appsettings.Production.json)
├─ Database.ConnectionString (requires env var)
├─ Client.BaseUrl (requires env var)
└─ Cors.AllowedOrigins (requires env var)
```

---

## Configuration Keys Reference

### API Configuration

| Section | Key | Example Value | Where |
|---------|-----|----------------|-------|
| Database | ConnectionString | `Host=localhost;Database=botc;...` | Dev config or env var |
| Client | BaseUrl | `http://localhost:5000` | Dev config or env var |
| Cors | AllowedOrigins | `["http://localhost:5001"]` | Dev config or env var |
| SignalR | RoomLobbyHubPath | `/hubs/room-lobby` | Root config |

### Desktop Configuration

| Section | Key | Example Value | Where |
|---------|-----|----------------|-------|
| Api | BaseUrl | `http://localhost:5000` | Dev config or env var |

---

## Common Tasks

### Change Database Connection

**Development:**
1. Edit `appsettings.Development.json`
2. Update `Database.ConnectionString`
3. Restart application

**Production:**
```powershell
$env:ConnectionStrings__DefaultConnection = "Host=prod-db;Port=5432;Database=botc;Username=user;Password=***"
dotnet run
```

### Add CORS Origin for New Client

**Development:**
```json
// appsettings.Development.json
"Cors": {
  "AllowedOrigins": [
    "http://localhost:5001",
    "http://localhost:3000"  // Add new origin
  ]
}
```

**Production (via env var):**
```powershell
$env:Cors__AllowedOrigins__0 = "https://app.example.com"
$env:Cors__AllowedOrigins__1 = "https://newapp.example.com"
```

### Run in Production Mode Locally

```powershell
# Set environment
$env:ASPNETCORE_ENVIRONMENT = "Production"

# Set all required configuration via environment variables
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Database=botc;..."
$env:Client__BaseUrl = "http://localhost:5000"
$env:Cors__AllowedOrigins__0 = "http://localhost:5001"

# Run
dotnet run
```

### Deploy to Docker

```bash
docker-compose up -d
```

All configuration is in `docker-compose.yml` environment section.

---

## Troubleshooting

### API Won't Start - "Database configuration is required"

**Problem:** Missing database connection string

**Solution:**
```powershell
# Make sure environment variable is set
$env:ASPNETCORE_ENVIRONMENT = "Development"
# Or explicitly set connection string
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Database=botc;..."
```

### CORS Error in Browser - "Access-Control-Allow-Origin"

**Problem:** Client origin not allowed

**Solution:**
1. Check browser console for actual origin URL
2. Add it to `Cors.AllowedOrigins` in configuration
3. Restart API

**Example:**
```json
"Cors": {
  "AllowedOrigins": [
    "http://localhost:5001",
    "http://192.168.1.100:5001"  // Add if using IP
  ]
}
```

### Desktop Can't Connect to API - "Connection refused"

**Problem:** Wrong API URL configured

**Solution:**
```powershell
# Check Desktop configuration
$env:ASPNETCORE_ENVIRONMENT = "Development"
# Should use http://localhost:5000 from appsettings.Development.json

# Or override
$env:Api__BaseUrl = "http://192.168.1.100:5000"
dotnet run
```

---

## File Locations

### Configuration Models

- API: `src/BOTC.Presentation.Api/Configuration/`
  - `DatabaseOptions.cs`
  - `ClientOptions.cs`
  - `CorsOptions.cs`
  - `SignalROptions.cs`

- Desktop: `src/BOTC.Presentation.Desktop/Configuration/`
  - `ApiOptions.cs`

### Configuration Files

**API:**
- `src/BOTC.Presentation.Api/appsettings.json`
- `src/BOTC.Presentation.Api/appsettings.Development.json`
- `src/BOTC.Presentation.Api/appsettings.Production.json`

**Desktop:**
- `src/BOTC.Presentation.Desktop/appsettings.json`
- `src/BOTC.Presentation.Desktop/appsettings.Development.json`
- `src/BOTC.Presentation.Desktop/appsettings.Production.json`

### Code Changes

- `src/BOTC.Presentation.Api/Program.cs`
- `src/BOTC.Infrastructure/InfrastructureServiceRegistration.cs`
- `src/BOTC.Presentation.Desktop/DesktopPresentationServiceRegistration.cs`

### Documentation

- `CONFIGURATION_REFACTORING.md` - Comprehensive overview
- `ENVIRONMENT_VARIABLES.md` - Detailed environment variable reference
- `CONFIGURATION_EXAMPLES.md` - Real-world usage examples
- `REFACTORING_SUMMARY.md` - Summary of all changes
- `IMPLEMENTATION_CHECKLIST.md` - Verification checklist
- `QUICK_START.md` - This file

---

## Key Points to Remember

✅ **Always set `ASPNETCORE_ENVIRONMENT`** - Controls which config file loads
✅ **Development mode is default** - Use `appsettings.Development.json` values
✅ **Environment variables override files** - Use for production secrets
✅ **Double underscore for nested keys** - `Section__Property`
✅ **CORS must be explicitly allowed** - Add your client origins to `AllowedOrigins`
✅ **No hardcoded values remain** - All configuration is externalized

---

## What Changed?

### Before (Hardcoded)
```csharp
// Program.cs - OLD
const string defaultConnectionStringName = "DefaultConnection";
var connectionString = builder.Configuration.GetConnectionString(defaultConnectionStringName);
builder.Services.AddInfrastructure(connectionString);

// DesktopPresentationServiceRegistration.cs - OLD
private static readonly Uri DefaultRoomsApiBaseAddress = new("http://localhost:5000");
```

### After (Configured)
```csharp
// Program.cs - NEW
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection("Database"));
builder.Services.AddInfrastructure(builder.Configuration);

// DesktopPresentationServiceRegistration.cs - NEW
services.Configure<ApiOptions>(configuration.GetSection("Api"));
var roomsApiBaseAddress = ResolveRoomsApiBaseAddress(apiOptions);
```

---

## Next Steps

1. **For Development:** Start with `appsettings.Development.json` - it's pre-configured
2. **For Testing:** See `CONFIGURATION_EXAMPLES.md` section on testing
3. **For Deployment:** See `ENVIRONMENT_VARIABLES.md` for environment setup
4. **For Questions:** Check `CONFIGURATION_REFACTORING.md` for detailed info

---

## Support

For detailed information, see:
- Architecture decisions → `REFACTORING_SUMMARY.md`
- Environment variables → `ENVIRONMENT_VARIABLES.md`
- Usage examples → `CONFIGURATION_EXAMPLES.md`
- Implementation details → `IMPLEMENTATION_CHECKLIST.md`

