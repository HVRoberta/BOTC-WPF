# Desktop Endpoint Configuration

The desktop client loads endpoint configuration in this order:

1. `appsettings.json`
2. `appsettings.{BOTC_ENVIRONMENT}.json` (`Local` by default)
3. `BOTC_` prefixed environment variables
4. Command-line switches

## Required settings

```json
{
  "Api": {
    "BaseUrl": "https://localhost:5001"
  },
  "SignalR": {
    "HubUrl": "https://localhost:5001/hubs/room-lobby"
  }
}
```

## Environment profiles

- Local: `appsettings.Local.json`
- Cloud: `appsettings.Cloud.json`

Set profile with environment variable:

```powershell
$env:BOTC_ENVIRONMENT = "Local"
$env:BOTC_ENVIRONMENT = "Cloud"
```

## Optional command-line overrides

```powershell
--api-base-url https://localhost:5001
--signalr-hub-url https://localhost:5001/hubs/room-lobby
```

