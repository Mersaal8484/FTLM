using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using Npgsql;

// Launches the actual API in explicitly gated Testing-only mode against disposable PostgreSQL.
internal static class ApiIsolationProof
{
    public static async Task RunAsync(string database, byte[] key, Guid tenantId)
    {
        var directory = Path.Combine(Path.GetTempPath(), "api-proof-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var keyFile = Path.Combine(directory, "test-key-1.key");
        await File.WriteAllTextAsync(keyFile, Convert.ToBase64String(key));
        File.SetUnixFileMode(keyFile, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        using var process = new Process { StartInfo = new ProcessStartInfo("dotnet", "/api/FuelTrack.API.dll")
        {
            UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true,
            WorkingDirectory = "/api"
        } };
        var env = process.StartInfo.Environment;
        env["ASPNETCORE_ENVIRONMENT"] = "Testing";
        env["ASPNETCORE_URLS"] = $"http://127.0.0.1:{port}";
        env["MultiTenancy__Enabled"] = "true";
        env["MultiTenancy__DisposableProof"] = "true";
        env["ConnectionStrings__Platform"] = database;
        env["ConnectionStrings__TenantDatabase"] = database;
        env["TenantCredentials__KeyDirectory"] = directory;
        env["JwtSettings__Key"] = "proof-only-signing-key-with-at-least-32-characters!";
        env["JwtSettings__Issuer"] = "proof-issuer";
        env["JwtSettings__Audience"] = "proof-client";
        env["DataEncryption__Key"] = "proof-only-key";
        process.OutputDataReceived += (_, _) => { };
        process.ErrorDataReceived += (_, _) => { };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        try
        {
            using var client = new HttpClient(new HttpClientHandler { UseCookies = false }) { BaseAddress = new Uri($"http://127.0.0.1:{port}") };
            HttpRequestMessage Request(HttpMethod method, string path, string host)
            {
                var request = new HttpRequestMessage(method, path);
                request.Headers.Host = host;
                return request;
            }
            HttpResponseMessage? response = null;
            for (var attempt = 0; attempt < 100; attempt++)
            {
                if (process.HasExited) throw new InvalidOperationException("API proof process stopped before starting.");
                try { response = await client.SendAsync(Request(HttpMethod.Get, "/api/auth/me", "provision.example.test")); break; }
                catch (HttpRequestException) { await Task.Delay(100); }
            }
            if (response is null) throw new InvalidOperationException("API proof process did not start.");
            response.Dispose();
            using var login = Request(HttpMethod.Post, "/api/auth/login", "provision.example.test");
            login.Content = JsonContent.Create(new { userName = "tenant-admin@example.test", password = "Tenant-Admin-Password-123!" });
            using var loggedIn = await client.SendAsync(login);
            if (loggedIn.StatusCode != HttpStatusCode.OK)
                throw new InvalidOperationException($"Tenant login failed with {(int)loggedIn.StatusCode}.");
            using var payload = JsonDocument.Parse(await loggedIn.Content.ReadAsStringAsync());
            var token = payload.RootElement.GetProperty("token").GetString() ?? throw new InvalidOperationException("Login token missing.");
            async Task<HttpStatusCode> Get(string host)
            {
                using var request = Request(HttpMethod.Get, "/api/auth/me", host);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                using var result = await client.SendAsync(request);
                return result.StatusCode;
            }
            if (await Get("provision.example.test") != HttpStatusCode.OK)
                throw new InvalidOperationException("Authenticated tenant profile failed.");
            if (await Get("proof.example.test") != HttpStatusCode.Forbidden)
                throw new InvalidOperationException("Token was accepted on another active tenant.");
            if (await Get("unknown.example.test") != HttpStatusCode.NotFound)
                throw new InvalidOperationException("Unknown tenant host was accepted.");
            using var createBranch = Request(HttpMethod.Post, "/api/branches", "provision.example.test");
            createBranch.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            createBranch.Content = JsonContent.Create(new { name = "API proof branch", code = "APIPROOF", isActive = true });
            using var branch = await client.SendAsync(createBranch);
            if (branch.StatusCode != HttpStatusCode.Created)
                throw new InvalidOperationException($"Tenant branch creation failed with {(int)branch.StatusCode}.");
            using var branchPayload = JsonDocument.Parse(await branch.Content.ReadAsStringAsync());
            var branchId = branchPayload.RootElement.GetProperty("id").GetInt32();
            using var createStation = Request(HttpMethod.Post, "/api/stations", "provision.example.test");
            createStation.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            createStation.Content = JsonContent.Create(new { branchId, name = "API proof station", code = "APISTATION", isActive = true });
            using var station = await client.SendAsync(createStation);
            if (station.StatusCode != HttpStatusCode.Created)
                throw new InvalidOperationException($"Tenant station creation failed with {(int)station.StatusCode}.");
            using var register = Request(HttpMethod.Post, "/api/auth/register", "provision.example.test");
            register.Content = JsonContent.Create(new { userName = "unwanted@example.test", email = "unwanted@example.test", fullName = "Unwanted", password = "Password-123!" });
            using var registration = await client.SendAsync(register);
            if (registration.StatusCode != HttpStatusCode.NotFound)
                throw new InvalidOperationException("Anonymous tenant registration was accepted.");
            await using var connection = new NpgsqlConnection(database);
            await connection.OpenAsync();
            var schema = "tenant_" + tenantId.ToString("N");
            await using (var audit = new NpgsqlCommand($"SELECT count(*) FROM \"{schema}\".\"AuditLogs\" WHERE \"EntityName\" = 'Stations' AND \"Action\" = 'Create'", connection))
                if ((long)(await audit.ExecuteScalarAsync())! != 1) throw new InvalidOperationException("Tenant audit record was not saved.");
            await using (var leakage = new NpgsqlCommand("SELECT count(*) FROM tenant_00000000000000000000000000000001.\"Stations\" WHERE \"Code\" = 'APISTATION'", connection))
                if ((long)(await leakage.ExecuteScalarAsync())! != 0) throw new InvalidOperationException("Station data crossed tenant schemas.");
            await using (var expire = new NpgsqlCommand("UPDATE platform.\"Subscriptions\" SET \"EndsAt\" = now() - interval '1 second' WHERE \"TenantId\" = @id", connection))
            {
                expire.Parameters.AddWithValue("id", tenantId);
                await expire.ExecuteNonQueryAsync();
            }
            if (await Get("provision.example.test") != HttpStatusCode.NotFound)
                throw new InvalidOperationException("Expired subscription retained access.");
            await using (var restore = new NpgsqlCommand("UPDATE platform.\"Subscriptions\" SET \"EndsAt\" = now() + interval '1 day' WHERE \"TenantId\" = @id", connection))
            {
                restore.Parameters.AddWithValue("id", tenantId);
                await restore.ExecuteNonQueryAsync();
            }
            Console.WriteLine("PASS: actual API tenant login, CRUD audit, host binding, registration and subscription expiry.");
        }
        finally
        {
            if (!process.HasExited) { process.Kill(entireProcessTree: true); await process.WaitForExitAsync(); }
            Directory.Delete(directory, recursive: true);
        }
    }
}
