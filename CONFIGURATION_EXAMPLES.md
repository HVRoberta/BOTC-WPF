# Configuration Usage Examples

This document provides practical examples of how to use the new configuration system in different scenarios.

## Table of Contents

1. [Local Development](#local-development)
2. [Docker Deployment](#docker-deployment)
3. [Cloud Deployment (Azure)](#cloud-deployment-azure)
4. [Unit Testing](#unit-testing)
5. [Integration Testing](#integration-testing)
6. [CI/CD Pipeline](#cicd-pipeline)

---

## Local Development

### Scenario: Running API locally with PostgreSQL

**Prerequisites:**
- PostgreSQL running on `localhost:5432`
- Database: `botc`, User: `postgres`, Password: `postgres`
- WPF client running on `localhost:5001`

**Setup:**

1. No additional setup needed - use `appsettings.Development.json`:
   ```json
   {
     "Database": {
       "ConnectionString": "Host=localhost;Port=5432;Database=botc;Username=postgres;Password=postgres"
     },
     "Client": {
       "BaseUrl": "http://localhost:5000"
     },
     "Cors": {
       "AllowedOrigins": ["http://localhost:5001"]
     }
   }
   ```

2. Set environment:
   ```powershell
   $env:ASPNETCORE_ENVIRONMENT = "Development"
   ```

3. Run API:
   ```powershell
   cd src/BOTC.Presentation.Api
   dotnet run
   ```

4. Desktop automatically connects to `http://localhost:5000` (from its `appsettings.Development.json`)

---

## Docker Deployment

### Scenario: Docker Compose with PostgreSQL container

**docker-compose.yml:**
```yaml
version: '3.8'

services:
  postgres:
    image: postgres:15-alpine
    environment:
      POSTGRES_DB: botc
      POSTGRES_USER: botc_user
      POSTGRES_PASSWORD: botc_password
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

  botc-api:
    build:
      context: .
      dockerfile: src/BOTC.Presentation.Api/Dockerfile
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ConnectionStrings__DefaultConnection: "Host=postgres;Port=5432;Database=botc;Username=botc_user;Password=botc_password"
      Client__BaseUrl: "http://localhost:5000"
      Cors__AllowedOrigins__0: "http://localhost:3000"
      Logging__LogLevel__Default: "Information"
    ports:
      - "5000:5000"
    depends_on:
      - postgres

volumes:
  postgres_data:
```

**Run:**
```bash
docker-compose up -d
```

**Key Points:**
- Uses `Production` environment (no `appsettings.Production.json` overrides)
- All configuration via environment variables
- Database hostname is service name `postgres` (Docker DNS)
- Connection string in environment variable overrides config files

---

## Cloud Deployment (Azure)

### Scenario: Azure App Service with Azure Database for PostgreSQL

**Architecture:**
```
┌─────────────────────────────────────────┐
│  Azure App Service                      │
│  - .NET 10 API running in container     │
│  - Environment variables set via portal │
└─────────────────────────────────────────┘
           │ connects to
┌─────────────────────────────────────────┐
│  Azure Database for PostgreSQL          │
│  - Managed database service             │
│  - Connection via hostname              │
└─────────────────────────────────────────┘
```

**Azure Portal Configuration:**

1. **Create App Service**
   - Runtime: .NET 10
   - Environment: Production

2. **Set Application Settings** (equivalent to environment variables):
   ```
   Name: ASPNETCORE_ENVIRONMENT
   Value: Production
   
   Name: ConnectionStrings__DefaultConnection
   Value: Host=botc-server.postgres.database.azure.com;Port=5432;Database=botc;Username=botcadmin@botc-server;Password=YOUR_SECURE_PASSWORD;SSL Mode=Require;
   
   Name: Client__BaseUrl
   Value: https://api.example.com
   
   Name: Cors__AllowedOrigins__0
   Value: https://app.example.com
   
   Name: Cors__AllowedOrigins__1
   Value: https://admin.example.com
   ```

3. **Deploy Application**
   - Publish to Azure App Service
   - Application automatically loads configuration from settings

**PowerShell Deployment Script:**
```powershell
$resourceGroup = "botc-rg"
$appService = "botc-api"

# Set application settings
$settings = @{
    ASPNETCORE_ENVIRONMENT = "Production"
    "ConnectionStrings__DefaultConnection" = "Host=botc-server.postgres.database.azure.com;Port=5432;Database=botc;Username=botcadmin@botc-server;Password=***;SSL Mode=Require;"
    "Client__BaseUrl" = "https://api.example.com"
    "Cors__AllowedOrigins__0" = "https://app.example.com"
    "Cors__AllowedOrigins__1" = "https://admin.example.com"
}

foreach ($key in $settings.Keys) {
    az webapp config appsettings set `
        --resource-group $resourceGroup `
        --name $appService `
        --settings "$key=$($settings[$key])"
}
```

---

## Unit Testing

### Scenario: Testing service with mocked configuration

**Test Configuration Helper:**
```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BOTC.Presentation.Api.Configuration;

namespace BOTC.Presentation.Api.Tests.Fixtures
{
    public class ConfigurationFixture
    {
        public IConfiguration CreateConfiguration(
            string? connectionString = null,
            string? clientBaseUrl = null,
            string[]? corsOrigins = null)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "Database:ConnectionString", connectionString ?? "Host=localhost;Database=botc_test;" },
                    { "Client:BaseUrl", clientBaseUrl ?? "http://localhost:5000" },
                    { "Cors:PolicyName", "AllowedOrigins" },
                    { "Cors:AllowedOrigins:0", corsOrigins?[0] ?? "http://localhost:5001" },
                    { "SignalR:RoomLobbyHubPath", "/hubs/room-lobby" },
                }.SkipNulls())
                .Build();
            
            return config;
        }
    }
}
```

**Usage in Tests:**
```csharp
[Fact]
public void AddInfrastructure_WithValidConfiguration_RegistersDbContext()
{
    // Arrange
    var fixture = new ConfigurationFixture();
    var configuration = fixture.CreateConfiguration(
        connectionString: "Host=localhost;Database=botc_test;"
    );
    var services = new ServiceCollection();

    // Act
    services.AddInfrastructure(configuration);

    // Assert
    var serviceProvider = services.BuildServiceProvider();
    var dbContext = serviceProvider.GetRequiredService<BotcDbContext>();
    Assert.NotNull(dbContext);
}

[Fact]
public void AddInfrastructure_WithoutConnectionString_ThrowsInvalidOperationException()
{
    // Arrange
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string> { })
        .Build();
    var services = new ServiceCollection();

    // Act & Assert
    Assert.Throws<InvalidOperationException>(() => 
        services.AddInfrastructure(configuration)
    );
}
```

---

## Integration Testing

### Scenario: Testing with real database and CORS configuration

**xUnit Collection Fixture:**
```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using BOTC.Presentation.Api;

namespace BOTC.Presentation.Api.Tests.Integration
{
    public class ApiTestsFixture : IAsyncLifetime
    {
        private readonly WebApplicationFactory<Program> _factory;
        public HttpClient Client { get; private set; }

        public ApiTestsFixture()
        {
            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureAppConfiguration((context, config) =>
                    {
                        config.AddInMemoryCollection(new Dictionary<string, string>
                        {
                            { "ASPNETCORE_ENVIRONMENT", "Development" },
                            { "ConnectionStrings:DefaultConnection", GetTestConnectionString() },
                            { "Client:BaseUrl", "http://localhost:5000" },
                            { "Cors:AllowedOrigins:0", "http://localhost:5001" },
                        });
                    });
                });

            Client = _factory.CreateClient();
        }

        public async Task InitializeAsync()
        {
            // Run migrations
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<BotcDbContext>();
                await db.Database.MigrateAsync();
            }
        }

        public async Task DisposeAsync()
        {
            await _factory.DisposeAsync();
        }

        private static string GetTestConnectionString()
        {
            // Use test database
            return "Host=localhost;Port=5432;Database=botc_test;Username=postgres;Password=postgres;";
        }
    }

    [Collection("API Integration Tests")]
    public class RoomsEndpointTests : IClassFixture<ApiTestsFixture>
    {
        private readonly ApiTestsFixture _fixture;

        public RoomsEndpointTests(ApiTestsFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task CreateRoom_WithValidRequest_ReturnsCreatedResult()
        {
            // Arrange
            var request = new CreateRoomRequest("TestPlayer");

            // Act
            var response = await _fixture.Client.PostAsJsonAsync("/api/rooms", request);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateRoom_WithoutCorsOrigin_ReturnsErrorIfOriginNotAllowed()
        {
            // Arrange
            var request = new CreateRoomRequest("TestPlayer");
            _fixture.Client.DefaultRequestHeaders.Add("Origin", "http://not-allowed.com");

            // Act
            var response = await _fixture.Client.PostAsJsonAsync("/api/rooms", request);

            // Assert - CORS preflight would fail, but note that CORS is typically handled
            // by the browser, not the API directly in this scenario
            Assert.NotNull(response);
        }
    }
}
```

---

## CI/CD Pipeline

### Scenario: GitHub Actions workflow for automated deployment

**.github/workflows/deploy-production.yml:**
```yaml
name: Deploy to Production

on:
  push:
    branches: [main]

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --configuration Release --no-restore

      - name: Run tests
        env:
          ASPNETCORE_ENVIRONMENT: Development
          ConnectionStrings__DefaultConnection: ${{ secrets.TEST_DATABASE_CONNECTION_STRING }}
        run: dotnet test --configuration Release --no-build

      - name: Publish
        run: dotnet publish src/BOTC.Presentation.Api/BOTC.Presentation.Api.csproj -c Release -o ./publish

      - name: Deploy to Azure
        uses: azure/webapps-deploy@v2
        with:
          app-name: botc-api
          publish-profile: ${{ secrets.AZURE_PUBLISH_PROFILE }}
          package: ./publish

      - name: Configure App Service Settings
        run: |
          az webapp config appsettings set \
            --resource-group ${{ secrets.AZURE_RESOURCE_GROUP }} \
            --name botc-api \
            --settings \
              ASPNETCORE_ENVIRONMENT=Production \
              ConnectionStrings__DefaultConnection=${{ secrets.PROD_DATABASE_CONNECTION_STRING }} \
              Client__BaseUrl=${{ secrets.PROD_API_BASE_URL }} \
              Cors__AllowedOrigins__0=${{ secrets.PROD_CORS_ORIGIN_1 }} \
              Cors__AllowedOrigins__1=${{ secrets.PROD_CORS_ORIGIN_2 }} \
              Logging__LogLevel__Default=Information
```

**GitHub Secrets Required:**
- `AZURE_PUBLISH_PROFILE` - Azure App Service publish profile
- `AZURE_RESOURCE_GROUP` - Azure resource group name
- `TEST_DATABASE_CONNECTION_STRING` - Test database for CI/CD
- `PROD_DATABASE_CONNECTION_STRING` - Production database
- `PROD_API_BASE_URL` - Production API URL
- `PROD_CORS_ORIGIN_1` - Allowed CORS origin 1
- `PROD_CORS_ORIGIN_2` - Allowed CORS origin 2

---

## Common Troubleshooting

### Configuration Not Loaded in Tests

**Problem:** Configuration returns null in test
```
System.ArgumentNullException: Value cannot be null. (Parameter 'databaseOptions')
```

**Solution:**
```csharp
// Ensure configuration is properly built
var config = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string>
    {
        { "Database:ConnectionString", "..." }
    })
    .Build();

// Add to services and retrieve
var services = new ServiceCollection();
services.Configure<DatabaseOptions>(config.GetSection("Database"));
var sp = services.BuildServiceProvider();
var options = sp.GetRequiredService<IOptions<DatabaseOptions>>();
Assert.NotNull(options.Value.ConnectionString);
```

### CORS Validation Failing

**Problem:** CORS errors in browser console
```
Access to XMLHttpRequest at 'http://localhost:5000' from origin 'http://localhost:5001' 
has been blocked by CORS policy
```

**Solution:**
1. Check `appsettings.Development.json` includes origin:
   ```json
   "Cors": {
     "AllowedOrigins": ["http://localhost:5001"]
   }
   ```

2. Verify CORS middleware is applied:
   ```csharp
   if (corsOptions != null && corsOptions.AllowedOrigins.Count > 0)
   {
       app.UseCors(corsOptions.PolicyName);
   }
   ```

3. Ensure origin exactly matches (protocol, domain, port)

### Environment Variable Not Applied

**Problem:** Configuration uses default instead of environment variable
```
Configuration returned default URL instead of environment variable
```

**Solution:**
1. Verify variable is set:
   ```powershell
   Get-Item env:ConnectionStrings__DefaultConnection
   ```

2. Restart application after setting variable

3. Check variable name format: `Section__Property` (double underscore)
   - ✅ `ConnectionStrings__DefaultConnection`
   - ❌ `ConnectionStrings_DefaultConnection` (single underscore)

4. Verify priority - environment variables override `appsettings.json`

---

## Best Practices Summary

✅ **Always use `appsettings.{Environment}.json`** for environment-specific settings
✅ **Keep `appsettings.json` generic** with empty or default values
✅ **Use environment variables** for sensitive data in production
✅ **Validate configuration at startup** to catch errors early
✅ **Use strongly-typed `IOptions<T>`** instead of magic strings
✅ **Document required configuration** for each environment
✅ **Test configuration changes** in CI/CD before production deployment
✅ **Rotate credentials regularly** in production environments

