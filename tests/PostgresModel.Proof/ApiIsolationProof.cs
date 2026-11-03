using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using Npgsql;
using OtpNet;

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
            await using (var brandingConnection = new NpgsqlConnection(database))
            {
                await brandingConnection.OpenAsync();
                await using var setBranding = new NpgsqlCommand("UPDATE platform.\"Tenants\" SET \"DisplayName\" = 'Visible brand', \"Logo\" = @logo, \"LogoContentType\" = 'image/png' WHERE \"Id\" = @id", brandingConnection);
                setBranding.Parameters.AddWithValue("id", tenantId);
                setBranding.Parameters.AddWithValue("logo", new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 1, 2 });
                await setBranding.ExecuteNonQueryAsync();
            }
            using var brandA = await client.SendAsync(Request(HttpMethod.Get, "/api/tenant-branding", "provision.example.test"));
            using var brandB = await client.SendAsync(Request(HttpMethod.Get, "/api/tenant-branding", "second.localhost"));
            if (brandA.StatusCode != HttpStatusCode.OK || brandB.StatusCode != HttpStatusCode.OK) throw new InvalidOperationException("Tenant branding request failed.");
            using var brandAJson = JsonDocument.Parse(await brandA.Content.ReadAsStringAsync());
            using var brandBJson = JsonDocument.Parse(await brandB.Content.ReadAsStringAsync());
            if (brandAJson.RootElement.GetProperty("name").GetString() != "Visible brand" ||
                brandBJson.RootElement.GetProperty("name").GetString() != "Second local tenant" ||
                brandAJson.RootElement.GetProperty("logoUrl").GetString() != "/api/tenant-branding/logo" ||
                (brandBJson.RootElement.TryGetProperty("logoUrl", out var otherLogo) && otherLogo.ValueKind != JsonValueKind.Null))
                throw new InvalidOperationException("Tenant branding leaked across domains.");
            using var logoA = await client.SendAsync(Request(HttpMethod.Get, "/api/tenant-branding/logo", "provision.example.test"));
            using var logoB = await client.SendAsync(Request(HttpMethod.Get, "/api/tenant-branding/logo", "second.localhost"));
            if (logoA.StatusCode != HttpStatusCode.OK || logoA.Content.Headers.ContentType?.MediaType != "image/png" ||
                logoB.StatusCode != HttpStatusCode.NotFound || !(await logoA.Content.ReadAsByteArrayAsync()).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 1, 2 }))
                throw new InvalidOperationException("Tenant logo isolation failed.");
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
            if (await Get("second.localhost") != HttpStatusCode.Forbidden)
                throw new InvalidOperationException("Token was accepted on a different local tenant domain.");
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
            using var setup = Request(HttpMethod.Post, "/api/auth/2fa/setup", "provision.example.test");
            setup.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            using var setupResult = await client.SendAsync(setup);
            if (setupResult.StatusCode != HttpStatusCode.OK) throw new InvalidOperationException($"2FA setup failed: {(int)setupResult.StatusCode}.");
            using var setupPayload = JsonDocument.Parse(await setupResult.Content.ReadAsStringAsync());
            var secret = setupPayload.RootElement.GetProperty("secretKey").GetString()!;
            var recovery = setupPayload.RootElement.GetProperty("backupCodes")[0].GetString()!;
            await using (var stored = new NpgsqlCommand($"SELECT \"TwoFactorSecret\" FROM \"{schema}\".\"Users\" WHERE \"Email\" = 'tenant-admin@example.test'", connection))
            {
                var encrypted = (string)(await stored.ExecuteScalarAsync())!;
                if (!encrypted.StartsWith("ft2fa1:") || encrypted.Contains(secret, StringComparison.Ordinal))
                    throw new InvalidOperationException("2FA secret was not encrypted at rest.");
            }
            var code = new Totp(Base32Encoding.ToBytes(secret)).ComputeTotp();
            using var enable = Request(HttpMethod.Post, "/api/auth/2fa/enable", "provision.example.test");
            enable.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            enable.Content = JsonContent.Create(new { code });
            using var enabled = await client.SendAsync(enable);
            if (enabled.StatusCode != HttpStatusCode.OK) throw new InvalidOperationException($"2FA enable failed: {(int)enabled.StatusCode}.");
            using var repeatSetup = Request(HttpMethod.Post, "/api/auth/2fa/setup", "provision.example.test");
            repeatSetup.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            using var repeatSetupResult = await client.SendAsync(repeatSetup);
            if (repeatSetupResult.StatusCode != HttpStatusCode.BadRequest) throw new InvalidOperationException("Enabled 2FA secret could be replaced without verification.");
            async Task<string> Challenge()
            {
                using var request = Request(HttpMethod.Post, "/api/auth/login", "provision.example.test");
                request.Content = JsonContent.Create(new { userName = "tenant-admin@example.test", password = "Tenant-Admin-Password-123!" });
                using var result = await client.SendAsync(request);
                if (result.StatusCode != HttpStatusCode.OK) throw new InvalidOperationException($"2FA challenge failed: {(int)result.StatusCode}.");
                using var body = JsonDocument.Parse(await result.Content.ReadAsStringAsync());
                if (!body.RootElement.GetProperty("requiresTwoFactor").GetBoolean() || body.RootElement.TryGetProperty("token", out _))
                    throw new InvalidOperationException("First factor issued a full token.");
                return body.RootElement.GetProperty("tempToken").GetString()!;
            }
            async Task<HttpStatusCode> Complete(string challenge, string secondFactor)
            {
                using var request = Request(HttpMethod.Post, "/api/auth/login/2fa", "provision.example.test");
                request.Content = JsonContent.Create(new { tempToken = challenge, code = secondFactor });
                using var result = await client.SendAsync(request);
                if (result.StatusCode == HttpStatusCode.OK)
                {
                    using var body = JsonDocument.Parse(await result.Content.ReadAsStringAsync());
                    if (string.IsNullOrWhiteSpace(body.RootElement.GetProperty("token").GetString())) throw new InvalidOperationException("2FA login token missing.");
                }
                return result.StatusCode;
            }
            if (await Complete(await Challenge(), code) != HttpStatusCode.OK) throw new InvalidOperationException("API rejected valid 2FA login.");
            if (await Complete(await Challenge(), recovery) != HttpStatusCode.OK) throw new InvalidOperationException("API rejected valid recovery code.");
            await using (var storedRecovery = new NpgsqlCommand($"SELECT \"TwoFactorBackupCodes\" FROM \"{schema}\".\"Users\" WHERE \"Email\" = 'tenant-admin@example.test'", connection))
            {
                var hashes = (string)(await storedRecovery.ExecuteScalarAsync())!;
                if (hashes.Contains(recovery, StringComparison.Ordinal) || FuelTrack.Application.Services.RecoveryCodes.Consume(hashes, recovery) is not null)
                    throw new InvalidOperationException("Recovery code was not securely consumed.");
            }
            using var disable = Request(HttpMethod.Post, "/api/auth/2fa/disable", "provision.example.test");
            disable.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            disable.Content = JsonContent.Create(new { code });
            using var disabled = await client.SendAsync(disable);
            if (disabled.StatusCode != HttpStatusCode.OK) throw new InvalidOperationException($"2FA disable failed: {(int)disabled.StatusCode}.");
            Console.WriteLine("PASS: API 2FA setup, encrypted secret, TOTP and recovery login.");
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
            await using (var suspend = new NpgsqlCommand("UPDATE platform.\"Tenants\" SET \"Status\" = 'Suspended' WHERE \"Id\" = @id", connection))
            {
                suspend.Parameters.AddWithValue("id", tenantId);
                await suspend.ExecuteNonQueryAsync();
            }
            if (await Get("provision.example.test") != HttpStatusCode.NotFound)
                throw new InvalidOperationException("Suspended tenant retained API access.");
            await using (var resume = new NpgsqlCommand("UPDATE platform.\"Tenants\" SET \"Status\" = 'Active' WHERE \"Id\" = @id", connection))
            {
                resume.Parameters.AddWithValue("id", tenantId);
                await resume.ExecuteNonQueryAsync();
            }
            if (await Get("provision.example.test") != HttpStatusCode.OK)
                throw new InvalidOperationException("Resumed tenant API access was not restored.");
            Console.WriteLine("PASS: actual API tenant login, CRUD audit, host binding, registration and subscription expiry.");
        }
        finally
        {
            if (!process.HasExited) { process.Kill(entireProcessTree: true); await process.WaitForExitAsync(); }
            Directory.Delete(directory, recursive: true);
        }
    }
}
