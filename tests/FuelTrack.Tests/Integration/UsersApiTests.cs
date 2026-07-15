using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FuelTrack.Application.DTOs.RBAC;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FuelTrack.Tests.Integration;

public class UsersApiTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ─────────────────────── Helpers ───────────────────────

    private async Task<int> SeedRoleAsync(string name, string? key = null)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();
        var role = new Role { Name = name, Key = key ?? name.ToLower(), IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        role.NormalizedName = name.ToUpperInvariant();
        db.Roles.Add(role);
        await db.SaveChangesAsync();
        return role.Id;
    }

    private async Task<int> SeedUserAsync(string userName, string fullName, string email, bool isActive = true)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();
        var hasher = new PasswordHasher<User>();
        var user = new User
        {
            UserName = userName,
            FullName = fullName,
            Email = email,
            NormalizedUserName = userName.ToUpper(),
            NormalizedEmail = email.ToUpper(),
            IsActive = isActive,
            SecurityStamp = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = 1
        };
        user.PasswordHash = hasher.HashPassword(user, "Test@123456");
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    // ─────────────── GET /api/users ───────────────

    [Fact]
    public async Task GetAll_Unauthenticated_ReturnsUnauthorized()
    {
        var client = Factory.CreateClient();
        var response = await client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_Authenticated_ReturnsOk()
    {
        var client = await CreateAuthenticatedClientAsync();
        var response = await client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<UserDto>>(JsonOpts);
        Assert.NotNull(body);
        Assert.True(body.Items.Count >= 1); // at least the seeded test admin
    }

    [Fact]
    public async Task GetAll_Pagination_WorksCorrectly()
    {
        var client = await CreateAuthenticatedClientAsync();
        // Seed extra users
        await SeedUserAsync("pageuser1", "Page User 1", "page1@test.com");
        await SeedUserAsync("pageuser2", "Page User 2", "page2@test.com");

        var response = await client.GetAsync("/api/users?page=1&pageSize=2");
        var body = await response.Content.ReadFromJsonAsync<PagedResult<UserDto>>(JsonOpts);
        Assert.NotNull(body);
        Assert.Equal(2, body.Items.Count);
        Assert.True(body.TotalCount >= 3);
    }

    // ─────────────── GET /api/users/{id} ───────────────

    [Fact]
    public async Task GetById_ExistingUser_ReturnsOk()
    {
        var client = await CreateAuthenticatedClientAsync();
        var userId = await SeedUserAsync("getbyiduser", "Get By ID", "getbyid@test.com");

        var response = await client.GetAsync($"/api/users/{userId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserDetailDto>(JsonOpts);
        Assert.NotNull(user);
        Assert.Equal("Get By ID", user.FullName);
        Assert.Equal("getbyid@test.com", user.Email);
    }

    [Fact]
    public async Task GetById_NonExistentUser_ReturnsNotFound()
    {
        var client = await CreateAuthenticatedClientAsync();
        var response = await client.GetAsync("/api/users/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ─────────────── POST /api/users (CREATE) ───────────────

    [Fact]
    public async Task Create_ValidUser_ReturnsCreated()
    {
        var client = await CreateAuthenticatedClientAsync();
        var dto = new CreateUserDto("newuser", "New User", "new@test.com", "1234567890", "Password123!", null, null);

        var response = await client.PostAsJsonAsync("/api/users", dto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserDto>(JsonOpts);
        Assert.NotNull(user);
        Assert.Equal("newuser", user.UserName);
        Assert.Equal("New User", user.FullName);

        // Verify DB
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();
        var dbUser = await db.Users.FirstOrDefaultAsync(u => u.UserName == "newuser");
        Assert.NotNull(dbUser);
        Assert.Equal("New User", dbUser.FullName);
    }

    [Fact]
    public async Task Create_ArabicFullName_ReturnsCreated()
    {
        var client = await CreateAuthenticatedClientAsync();
        var dto = new CreateUserDto("arabicuser", "محمد بن سلمان", "arabic@test.com", "0555123456", "Password123!", null, null);

        var response = await client.PostAsJsonAsync("/api/users", dto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserDto>(JsonOpts);
        Assert.NotNull(user);
        Assert.Equal("محمد بن سلمان", user.FullName);

        // Verify DB
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();
        var dbUser = await db.Users.FirstOrDefaultAsync(u => u.UserName == "arabicuser");
        Assert.NotNull(dbUser);
        Assert.Equal("محمد بن سلمان", dbUser.FullName);
    }

    [Fact]
    public async Task Create_WithRole_AssignedCorrectly()
    {
        var client = await CreateAuthenticatedClientAsync();
        var roleId = await SeedRoleAsync("مشرف الوقود", "Fuel_Sup");
        var dto = new CreateUserDto("roleuser", "Role User", "role@test.com", null, "Password123!", new List<string> { "Fuel_Sup" }, null);

        var response = await client.PostAsJsonAsync("/api/users", dto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Verify role assigned in DB
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();
        var dbUser = await db.Users.FirstOrDefaultAsync(u => u.UserName == "roleuser");
        Assert.NotNull(dbUser);
        var userRoles = await db.UserRoles.Where(ur => ur.UserId == dbUser.Id).ToListAsync();
        Assert.Single(userRoles);
        Assert.Equal(roleId, userRoles[0].RoleId);
    }

    [Fact]
    public async Task Create_WithMultipleRoles_AssignedCorrectly()
    {
        var client = await CreateAuthenticatedClientAsync();
        await SeedRoleAsync("مشرف الوقود", "Fuel_Sup");
        await SeedRoleAsync("مشغل", "oper");
        var dto = new CreateUserDto("multiroleuser", "Multi Role User", "multirole@test.com", null, "Password123!",
            new List<string> { "Fuel_Sup", "oper" }, null);

        var response = await client.PostAsJsonAsync("/api/users", dto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();
        var dbUser = await db.Users.FirstOrDefaultAsync(u => u.UserName == "multiroleuser");
        Assert.NotNull(dbUser);
        var userRoles = await db.UserRoles.Where(ur => ur.UserId == dbUser.Id).ToListAsync();
        Assert.Equal(2, userRoles.Count);
    }

    [Fact]
    public async Task Create_DuplicateUsername_ReturnsBadRequest()
    {
        var client = await CreateAuthenticatedClientAsync();
        await SeedUserAsync("dupuser", "Dup User", "dup@test.com");
        var dto = new CreateUserDto("dupuser", "Dup User 2", "dup2@test.com", null, "Password123!", null, null);

        var response = await client.PostAsJsonAsync("/api/users", dto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidPassword_ReturnsBadRequest()
    {
        var client = await CreateAuthenticatedClientAsync();
        var dto = new CreateUserDto("weakpwuser", "Weak PW", "weakpw@test.com", null, "123", null, null);

        var response = await client.PostAsJsonAsync("/api/users", dto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─────────────── PUT /api/users/{id} (UPDATE) ───────────────

    [Fact]
    public async Task Update_ExistingUser_ReturnsOk()
    {
        var client = await CreateAuthenticatedClientAsync();
        var userId = await SeedUserAsync("updateuser", "Old Name", "old@test.com");
        var dto = new UpdateUserDto("Updated Name", "updated@test.com", "9999999999", true);

        var response = await client.PutAsJsonAsync($"/api/users/{userId}", dto);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify DB
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();
        var dbUser = await db.Users.FindAsync(userId);
        Assert.NotNull(dbUser);
        Assert.Equal("Updated Name", dbUser.FullName);
        Assert.Equal("updated@test.com", dbUser.Email);
        Assert.Equal("9999999999", dbUser.PhoneNumber);
    }

    [Fact]
    public async Task Update_ArabicName_ReturnsOk()
    {
        var client = await CreateAuthenticatedClientAsync();
        var userId = await SeedUserAsync("arabicupdate", "English Name", "arabicupd@test.com");
        var dto = new UpdateUserDto("عبدالله بن عبدالعزيز", null, null, null);

        var response = await client.PutAsJsonAsync($"/api/users/{userId}", dto);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();
        var dbUser = await db.Users.FindAsync(userId);
        Assert.Equal("عبدالله بن عبدالعزيز", dbUser!.FullName);
    }

    [Fact]
    public async Task Update_DeactivateUser_Works()
    {
        var client = await CreateAuthenticatedClientAsync();
        var userId = await SeedUserAsync("deactuser", "Deact User", "deact@test.com");
        var dto = new UpdateUserDto(null, null, null, false);

        var response = await client.PutAsJsonAsync($"/api/users/{userId}", dto);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();
        var dbUser = await db.Users.FindAsync(userId);
        Assert.False(dbUser!.IsActive);
    }

    // ─────────────── DELETE /api/users/{id} ───────────────

    [Fact]
    public async Task Delete_ExistingUser_ReturnsNoContent()
    {
        var client = await CreateAuthenticatedClientAsync();
        var userId = await SeedUserAsync("deleteuser", "Delete User", "delete@test.com");

        var response = await client.DeleteAsync($"/api/users/{userId}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify soft-deleted (IsActive = false)
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();
        var dbUser = await db.Users.FindAsync(userId);
        Assert.NotNull(dbUser);
        Assert.False(dbUser.IsActive);
    }

    // ─────────────── POST /api/users/{id}/assign-roles ───────────────

    [Fact]
    public async Task AssignRoles_ValidRoles_ReturnsNoContent()
    {
        var client = await CreateAuthenticatedClientAsync();
        var userId = await SeedUserAsync("assignrole", "Assign Role", "assignrole@test.com");
        await SeedRoleAsync("مشرف الوقود", "Fuel_Sup");

        var response = await client.PostAsJsonAsync($"/api/users/{userId}/assign-roles", new List<string> { "Fuel_Sup" });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify DB
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();
        var userRoles = await db.UserRoles.Where(ur => ur.UserId == userId).ToListAsync();
        Assert.Single(userRoles);
    }

    [Fact]
    public async Task AssignRoles_ArabicRoleKey_WorksCorrectly()
    {
        var client = await CreateAuthenticatedClientAsync();
        var userId = await SeedUserAsync("arabicrole", "Arabic Role", "arabicrole@test.com");
        await SeedRoleAsync("مشغل", "oper");

        var response = await client.PostAsJsonAsync($"/api/users/{userId}/assign-roles", new List<string> { "oper" });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();
        var userRoles = await db.UserRoles.Where(ur => ur.UserId == userId).ToListAsync();
        Assert.Single(userRoles);
    }

    [Fact]
    public async Task AssignRoles_ReplacesExistingRoles()
    {
        var client = await CreateAuthenticatedClientAsync();
        var userId = await SeedUserAsync("replaceroles", "Replace Roles", "replace@test.com");
        await SeedRoleAsync("مشرف الوقود", "Fuel_Sup");
        await SeedRoleAsync("مشغل", "oper");

        // First assignment
        await client.PostAsJsonAsync($"/api/users/{userId}/assign-roles", new List<string> { "Fuel_Sup" });
        // Second assignment (should replace)
        var response = await client.PostAsJsonAsync($"/api/users/{userId}/assign-roles", new List<string> { "oper" });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();
        var userRoles = await db.UserRoles.Where(ur => ur.UserId == userId).ToListAsync();
        Assert.Single(userRoles); // Should only have oper, not Fuel_Sup
    }

    // ─────────────── GET /api/users/{id}/roles ───────────────

    [Fact]
    public async Task GetRoles_UserWithRoles_ReturnsOk()
    {
        var client = await CreateAuthenticatedClientAsync();
        var userId = await SeedUserAsync("getrolesuser", "Get Roles", "getroles@test.com");
        await SeedRoleAsync("مشرف الوقود", "Fuel_Sup");
        await client.PostAsJsonAsync($"/api/users/{userId}/assign-roles", new List<string> { "Fuel_Sup" });

        var response = await client.GetAsync($"/api/users/{userId}/roles");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var roles = await response.Content.ReadFromJsonAsync<List<string>>(JsonOpts);
        Assert.NotNull(roles);
        Assert.Single(roles);
    }

    [Fact]
    public async Task GetRoles_UserWithNoRoles_ReturnsEmptyList()
    {
        var client = await CreateAuthenticatedClientAsync();
        var userId = await SeedUserAsync("norolesuser", "No Roles", "noroles@test.com");

        var response = await client.GetAsync($"/api/users/{userId}/roles");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var roles = await response.Content.ReadFromJsonAsync<List<string>>(JsonOpts);
        Assert.NotNull(roles);
        Assert.Empty(roles);
    }

    // ─────────────── POST /api/users/{id}/activate ───────────────

    [Fact]
    public async Task Activate_InactiveUser_ReturnsNoContent()
    {
        var client = await CreateAuthenticatedClientAsync();
        var userId = await SeedUserAsync("activateuser", "Activate User", "activate@test.com", false);

        var response = await client.PostAsync($"/api/users/{userId}/activate", null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();
        var dbUser = await db.Users.FindAsync(userId);
        Assert.True(dbUser!.IsActive);
    }

    // ─────────────── POST /api/users/{id}/deactivate ───────────────

    [Fact]
    public async Task Deactivate_ActiveUser_ReturnsNoContent()
    {
        var client = await CreateAuthenticatedClientAsync();
        var userId = await SeedUserAsync("deactivateuser", "Deactivate User", "deactivate@test.com", true);

        var response = await client.PostAsync($"/api/users/{userId}/deactivate", null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();
        var dbUser = await db.Users.FindAsync(userId);
        Assert.False(dbUser!.IsActive);
    }

    // ─────────────── PATCH /api/users/{id}/reset-password ───────────────

    [Fact]
    public async Task ResetPassword_ValidPassword_ReturnsNoContent()
    {
        var client = await CreateAuthenticatedClientAsync();
        var userId = await SeedUserAsync("resetpwuser", "Reset PW", "resetpw@test.com");

        var response = await client.PatchAsJsonAsync($"/api/users/{userId}/reset-password",
            new { Password = "NewPassword123!" });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify user can login with new password
        var loginClient = Factory.CreateClient();
        var loginResponse = await loginClient.PostAsJsonAsync("/api/auth/login",
            new { UserName = "resetpwuser", Password = "NewPassword123!" });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    // ─────────────── POST /api/users/{id}/scopes ───────────────

    [Fact]
    public async Task AssignScopes_ValidScopes_ReturnsNoContent()
    {
        var client = await CreateAuthenticatedClientAsync();
        var userId = await SeedUserAsync("scopessuser", "Scopes User", "scopes@test.com");
        var dto = new AssignScopesDto(new List<UserScopeDto>
        {
            new(1, null, null) // Branch-level scope
        });

        var response = await client.PostAsJsonAsync($"/api/users/{userId}/scopes", dto);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify DB
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();
        var scopes = await db.UserScopes.Where(s => s.UserId == userId).ToListAsync();
        Assert.Single(scopes);
        Assert.Equal(1, scopes[0].BranchId);
    }

    [Fact]
    public async Task AssignScopes_GlobalScope_ReturnsNoContent()
    {
        var client = await CreateAuthenticatedClientAsync();
        var userId = await SeedUserAsync("globalscope", "Global Scope", "global@test.com");
        var dto = new AssignScopesDto(new List<UserScopeDto>
        {
            new(null, null, null) // Global scope (all access)
        });

        var response = await client.PostAsJsonAsync($"/api/users/{userId}/scopes", dto);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task AssignScopes_MultipleGlobalScopes_ReturnsBadRequest()
    {
        var client = await CreateAuthenticatedClientAsync();
        var userId = await SeedUserAsync("multiglobal", "Multi Global", "multiglobal@test.com");
        var dto = new AssignScopesDto(new List<UserScopeDto>
        {
            new(null, null, null),
            new(null, null, null) // Duplicate global
        });

        var response = await client.PostAsJsonAsync($"/api/users/{userId}/scopes", dto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AssignScopes_GlobalWithOtherScopes_ReturnsBadRequest()
    {
        var client = await CreateAuthenticatedClientAsync();
        var userId = await SeedUserAsync("globalwithother", "Global With Other", "globalwithother@test.com");
        var dto = new AssignScopesDto(new List<UserScopeDto>
        {
            new(null, null, null),
            new(1, null, null) // Global + branch
        });

        var response = await client.PostAsJsonAsync($"/api/users/{userId}/scopes", dto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AssignScopes_DuplicateScopes_ReturnsBadRequest()
    {
        var client = await CreateAuthenticatedClientAsync();
        var userId = await SeedUserAsync("dupscope", "Dup Scope", "dupscope@test.com");
        var dto = new AssignScopesDto(new List<UserScopeDto>
        {
            new(1, 2, null),
            new(1, 2, null) // Duplicate
        });

        var response = await client.PostAsJsonAsync($"/api/users/{userId}/scopes", dto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AssignScopes_SubStationWithoutStation_ReturnsBadRequest()
    {
        var client = await CreateAuthenticatedClientAsync();
        var userId = await SeedUserAsync("subnoalarm", "Sub No Alarm", "subnoalarm@test.com");
        var dto = new AssignScopesDto(new List<UserScopeDto>
        {
            new(null, null, 5) // SubStation without Station
        });

        var response = await client.PostAsJsonAsync($"/api/users/{userId}/scopes", dto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AssignScopes_StationWithoutBranch_ReturnsBadRequest()
    {
        var client = await CreateAuthenticatedClientAsync();
        var userId = await SeedUserAsync("statenobranch", "Station No Branch", "statenobranch@test.com");
        var dto = new AssignScopesDto(new List<UserScopeDto>
        {
            new(null, 3, null) // Station without Branch
        });

        var response = await client.PostAsJsonAsync($"/api/users/{userId}/scopes", dto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ─────────────── Full Create + Assign Roles + Verify DB ───────────────

    [Fact]
    public async Task FullFlow_CreateWithRole_VerifyEverythingInDB()
    {
        var client = await CreateAuthenticatedClientAsync();
        await SeedRoleAsync("مشرف الوقود", "Fuel_Sup");

        // 1. Create user with role
        var dto = new CreateUserDto("fullflow", "Full Flow User", "fullflow@test.com", "0555123456", "Password123!",
            new List<string> { "Fuel_Sup" }, null);
        var createResponse = await client.PostAsJsonAsync("/api/users", dto);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<UserDto>(JsonOpts);
        Assert.NotNull(created);

        // 2. Verify user exists in DB
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();
        var dbUser = await db.Users.FindAsync(created.Id);
        Assert.NotNull(dbUser);
        Assert.Equal("fullflow", dbUser.UserName);
        Assert.Equal("Full Flow User", dbUser.FullName);
        Assert.Equal("fullflow@test.com", dbUser.Email);
        Assert.Equal("0555123456", dbUser.PhoneNumber);
        Assert.True(dbUser.IsActive);

        // 3. Verify role assigned
        var userRoles = await db.UserRoles.Where(ur => ur.UserId == created.Id).ToListAsync();
        Assert.Single(userRoles);

        // 4. Get user by ID
        var getResponse = await client.GetAsync($"/api/users/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var detail = await getResponse.Content.ReadFromJsonAsync<UserDetailDto>(JsonOpts);
        Assert.NotNull(detail);
        Assert.Equal("Full Flow User", detail.FullName);

        // 5. Update user
        var updateDto = new UpdateUserDto("Full Flow Updated", null, "0555999999", null);
        var updateResponse = await client.PutAsJsonAsync($"/api/users/{created.Id}", updateDto);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        // 6. Verify update in DB
        await db.Entry(dbUser).ReloadAsync();
        Assert.Equal("Full Flow Updated", dbUser.FullName);
        Assert.Equal("0555999999", dbUser.PhoneNumber);
    }
}

// PagedResult helper (matches backend)
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
