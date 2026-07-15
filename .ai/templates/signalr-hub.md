# Template: SignalR Hub

## Metadata
- **Name**: signalr-hub
- **Purpose**: SignalR hub for real-time notifications
- **Layer**: API
- **Path**: `src/FuelTrack.API/Hubs/{Name}Hub.cs`

## Template

```csharp
using FuelTrack.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FuelTrack.API.Hubs;

[Authorize]
public class {Name}Hub : Hub
{
    private readonly ILogger<{Name}Hub> _logger;

    public {Name}Hub(ILogger<{Name}Hub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation("User {UserId} connected to {Hub}", userId, nameof({Name}Hub));
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        _logger.LogInformation("User {UserId} disconnected from {Hub}", userId, nameof({Name}Hub));
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinStationGroup(int stationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"station_{stationId}");
    }

    public async Task LeaveStationGroup(int stationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"station_{stationId}");
    }

    public async Task JoinSubStationGroup(int subStationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"substation_{subStationId}");
    }

    public async Task LeaveSubStationGroup(int subStationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"substation_{subStationId}");
    }
}
```

## Usage in Services

```csharp
// Notify station group
await _hubContext.Clients.Group($"station_{stationId}")
    .SendAsync("StationUpdated", stationDto);

// Notify user
await _hubContext.Clients.Group($"user_{userId}")
    .SendAsync("NotificationReceived", notificationDto);
```

## Checklist
- [ ] `[Authorize]` at class level
- [ ] User group assignment in `OnConnectedAsync`
- [ ] Station/SubStation group methods
- [ ] Proper cleanup in `OnDisconnectedAsync`
- [ ] Structured logging
- [ ] Hub registered in `Program.cs`
