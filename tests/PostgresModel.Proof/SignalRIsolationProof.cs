using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FuelTrack.API.Hubs;
using FuelTrack.API.Services;
using FuelTrack.Application.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

// Isolated Kestrel host: exercises actual WebSocket connections and the production hub
// and notifier. The test-only middleware supplies tenant context; it does not prove
// the complete API request pipeline or production deployment.
internal static class SignalRIsolationProof
{
    public static async Task RunAsync()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-only-signalr-signing-key-at-least-32-bytes"));
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Configuration["MultiTenancy:Enabled"] = "true";
        builder.Services.AddSignalR();
        builder.Services.AddScoped<ProofTenant>();
        builder.Services.AddScoped<ITenantRequestContext>(s => s.GetRequiredService<ProofTenant>());
        builder.Services.AddScoped<IUserScopeFilterService, ProofScopes>();
        builder.Services.AddAuthentication("SignalR").AddJwtBearer("SignalR", options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false, ValidateAudience = false, ValidateLifetime = true,
                ValidateIssuerSigningKey = true, IssuerSigningKey = key
            };
            options.Events = new JwtBearerEvents { OnMessageReceived = context =>
            {
                context.Token = context.Request.Query["access_token"];
                return Task.CompletedTask;
            } };
        });
        builder.Services.AddAuthorization();
        await using var app = builder.Build();
        app.UseAuthentication();
        app.Use(async (context, next) =>
        {
            if (Guid.TryParse(context.Request.Query["test_tenant"], out var id))
                context.RequestServices.GetRequiredService<ProofTenant>().Id = id;
            await next();
        });
        app.UseAuthorization();
        app.MapHub<FuelTrackHub>("/hubs/fueltrack");
        await app.StartAsync();
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            string Token(Guid tenant) => new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
                claims: [new Claim(ClaimTypes.NameIdentifier, "7"), new Claim("tenant_id", tenant.ToString())],
                expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)));
            async Task<ClientWebSocket> Connect(Guid contextTenant, Guid claimTenant)
            {
                var socket = new ClientWebSocket();
                var url = address.Replace("http://", "ws://") + "/hubs/fueltrack?test_tenant=" + contextTenant + "&access_token=" + Uri.EscapeDataString(Token(claimTenant));
                await socket.ConnectAsync(new Uri(url), CancellationToken.None);
                await Send(socket, "{\"protocol\":\"json\",\"version\":1}");
                if (await Receive(socket) != "{}") throw new InvalidOperationException("SignalR handshake failed.");
                return socket;
            }
            using var a = await Connect(tenantA, tenantA);
            using var b = await Connect(tenantB, tenantB);
            // Calling a hub method after handshake guarantees OnConnectedAsync completed.
            await Send(a, "{\"type\":1,\"invocationId\":\"a\",\"target\":\"JoinSubStationGroup\",\"arguments\":[3]}");
            await Send(b, "{\"type\":1,\"invocationId\":\"b\",\"target\":\"JoinSubStationGroup\",\"arguments\":[3]}");
            if (!IsCompletion(await Receive(a), "a") || !IsCompletion(await Receive(b), "b"))
                throw new InvalidOperationException("Joining the tenant SignalR groups failed.");

            async Task Notify(Guid id, string method, string data, int kind)
            {
                using var scope = app.Services.CreateScope();
                scope.ServiceProvider.GetRequiredService<ProofTenant>().Id = id;
                var notifier = new RealtimeNotifier(scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.SignalR.IHubContext<FuelTrackHub>>(),
                    scope.ServiceProvider.GetRequiredService<IConfiguration>(), scope.ServiceProvider.GetRequiredService<ITenantRequestContext>());
                if (kind == 0) await notifier.NotifyAllAsync(method, data);
                else if (kind == 1) await notifier.NotifyUserAsync("7", method, data);
                else await notifier.NotifyGroupAsync("substation_3", method, data);
            }
            for (var kind = 0; kind < 3; kind++)
            {
                await Notify(tenantA, "isolation", "only-a-" + kind, kind);
                await Notify(tenantB, "isolation", "only-b-" + kind, kind);
                if (!IsEvent(await Receive(a), "only-a-" + kind) || !IsEvent(await Receive(b), "only-b-" + kind))
                    throw new InvalidOperationException("A SignalR event crossed tenant boundaries.");
            }
            using var mismatch = await Connect(tenantA, tenantB);
            await Send(mismatch, "{\"type\":1,\"invocationId\":\"bad\",\"target\":\"JoinSubStationGroup\",\"arguments\":[3]}");
            try
            {
                var response = await Receive(mismatch);
                if (IsCompletion(response, "bad")) throw new InvalidOperationException("Mismatched tenant connected to SignalR.");
            }
            catch (WebSocketException) { /* hub rejected connection */ }
            Console.WriteLine("PASS: real SignalR WebSocket broadcast, user and group isolation.");
        }
        finally { await app.StopAsync(); }
    }

    private static async Task Send(ClientWebSocket socket, string value) =>
        await socket.SendAsync(Encoding.UTF8.GetBytes(value + "\u001e"), WebSocketMessageType.Text, true, CancellationToken.None);
    private static async Task<string> Receive(ClientWebSocket socket)
    {
        var buffer = new byte[8192];
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var result = await socket.ReceiveAsync(buffer, timeout.Token);
        if (result.MessageType == WebSocketMessageType.Close) throw new WebSocketException("SignalR connection closed.");
        return Encoding.UTF8.GetString(buffer, 0, result.Count).TrimEnd('\u001e');
    }
    private static bool IsCompletion(string json, string id)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("type", out var type) && type.GetInt32() == 3
            && doc.RootElement.GetProperty("invocationId").GetString() == id && !doc.RootElement.TryGetProperty("error", out _);
    }
    private static bool IsEvent(string json, string data)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("type").GetInt32() == 1
            && doc.RootElement.GetProperty("target").GetString() == "isolation"
            && doc.RootElement.GetProperty("arguments")[0].GetString() == data;
    }
    private sealed class ProofTenant : ITenantRequestContext
    {
        public Guid? Id { get; set; }
        public Guid? TenantId => Id;
        public string? DatabaseConnection => null;
    }
    private sealed class ProofScopes(ITenantRequestContext tenant) : IUserScopeFilterService
    {
        public Task<List<int>> GetAccessibleSubStationIdsAsync(int userId) => Task.FromResult(new List<int> { 3 });
        public Task<List<int>> GetAccessibleStationIdsAsync(int userId) => Task.FromResult(new List<int>());
        public Task<List<int>> GetAccessibleBranchIdsAsync(int userId) => Task.FromResult(new List<int>());
        public Task<List<int>> GetAccessibleMeterIdsAsync(int userId) => Task.FromResult(new List<int>());
        public Task<List<int>> GetUserIdsWithAccessToSubStationAsync(int subStationId) => Task.FromResult(new List<int>());
        public Task<bool> HasAccessToSubStationAsync(int userId, int subStationId) => Task.FromResult(tenant.TenantId.HasValue && subStationId == 3);
        public Task<bool> HasFullAccessToStationAsync(int userId, int stationId) => Task.FromResult(false);
    }
}
