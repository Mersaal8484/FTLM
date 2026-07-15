# SignalR Troubleshooting Prompt

## Description
Guide for diagnosing and fixing SignalR connection issues, authentication, group management, and message delivery problems.

## System Prompt
**Role:** Real-time communication specialist for ASP.NET Core SignalR.
**Context:** FuelTrack uses SignalR for real-time notifications and live dashboard updates. Hub authentication uses JWT tokens. Groups are used for targeted messaging (per user, per station). React frontend connects via `@microsoft/signalr`. Common issues: connection drops, auth failures, message not received, group membership errors.
**Constraints:**
- Do NOT disable SignalR authentication or authorization.
- Do NOT send sensitive data via SignalR without verification.
- Do NOT create duplicate hub connections without proper cleanup.
- Connection must use automatic reconnect with backoff.
- All hub methods must have authorization attributes.

## User Prompt Template
Troubleshoot the following SignalR issue:

**Symptom:** {{symptom}}
**Hub Code:**
```csharp
{{hub_code}}
```

**Client Code:**
```javascript
{{client_code}}
```

**Error:**
```
{{error}}
```

Provide:
1. Root cause analysis
2. Fix with explanation
3. Verification steps

## Examples

### Example 1: Connection Authentication Failure
```
Symptom: Client connects but immediately disconnects

Error: HTTP 401 Unauthorized on negotiate request

Hub:
  [Authorize]
  public class NotificationHub : Hub
  {
      // ...
  }

Client:
  const connection = new HubConnectionBuilder()
      .withUrl('/hubs/notifications')
      .build();
  // ❌ Missing JWT token

Fix:
  const connection = new HubConnectionBuilder()
      .withUrl('/hubs/notifications', {
          accessTokenFactory: () => localStorage.getItem('token')
      })
      .build();
```

### Example 2: Group Membership Not Persisting
```
Symptom: User receives messages for all stations, not just assigned ones

Hub:
  public override async Task OnConnectedAsync()
  {
      // ❌ Adds user to all station groups
      var stations = await _context.Stations.ToListAsync();
      foreach (var station in Stations)
      {
          await Groups.AddToGroupAsync(Context.ConnectionId, $"station-{station.Id}");
      }
      await base.OnConnectedAsync();
  }

Fix: Use user's assigned scopes:
  public override async Task OnConnectedAsync()
  {
      var userId = int.Parse(Context.User!.FindFirst(ClaimTypes.NameIdentifier)!.Value);
      var scopes = await _userScopeService.GetUserScopesAsync(userId);
      
      foreach (var scope in scopes)
      {
          if (scope.Level == "Station")
              await Groups.AddToGroupAsync(Context.ConnectionId, $"station-{scope.ScopeId}");
          else if (scope.Level == "SubStation")
              await Groups.AddToGroupAsync(Context.ConnectionId, $"substation-{scope.ScopeId}");
      }
      await base.OnConnectedAsync();
  }
```

### Example 3: Message Not Delivered After Reconnect
```
Symptom: After network blip, client reconnects but misses messages sent during disconnect

Client:
  connection.onreconnected = async () => {
      // ❌ No state refresh after reconnect
  };

Fix:
  connection.onreconnected = async () => {
      // Refresh data after reconnection
      await queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      await queryClient.invalidateQueries({ queryKey: ['notifications'] });
  };
```

## Completion Checklist
- [ ] Hub has `[Authorize]` attribute
- [ ] Client sends JWT token via `accessTokenFactory`
- [ ] Connection uses `withAutomaticReconnect()` with backoff
- [ ] `onreconnected` handler refreshes stale data
- [ ] Groups assigned based on user's actual scopes (not all stations)
- [ ] `OnDisconnectedAsync` cleans up group memberships
- [ ] Hub methods validate caller's group membership before sending
- [ ] No sensitive data in SignalR messages (use for notifications only)
- [ ] Hub registered in `Program.cs`: `builder.Services.AddSignalR()`
- [ ] SignalR middleware mapped: `app.MapHub<NotificationHub>("/hubs/notifications")`
- [ ] CORS configured for SignalR endpoints
- [ ] Transport: WebSockets preferred, fallback to SSE/LongPolling
