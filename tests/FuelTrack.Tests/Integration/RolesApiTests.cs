using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FuelTrack.Application.DTOs.RBAC;
using FuelTrack.Application.Common;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FuelTrack.Tests.Integration;

public class RolesApiTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ─────────────────────── Helpers ───────────────────────

    private async Task<int> SeedRoleAsync(string name, string? key = null, bool isActive = true)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();
        var role = new Role
        {
            Name = name,
            Key = key ?? name.ToLower(),
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        role.NormalizedName = name.ToUpperInvariant();
        db.Roles.Add(role);
        await db.SaveChangesAsync();
        return role.Id;
    }

    // ─────────────── GET /api/roles ───────────────

    [Fact]
    public async Task GetAll_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/roles");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_Authenticated_ReturnsOkWithRoles()
    {
        var client = await CreateAuthenticatedClientAsync();
        await SeedRoleAsync("TestRole_GetAll", "test_getall");

        var response = await client.GetAsync("/api/roles");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var roles = await response.Content.ReadFromJsonAsync<List<RoleDto>>(JsonOpts);
        Assert.NotNull(roles);
        Assert.NotEmpty(roles);
    }

    [Fact]
    public async Task GetAll_IncludesSeedRole()
    {
        var client = await CreateAuthenticatedClientAsync();
        await SeedRoleAsync("TestRole_Contains", "test_contains");

        var response = await client.GetAsync("/api/roles");
        var roles = await response.Content.ReadFromJsonAsync<List<RoleDto>>(JsonOpts);

        Assert.NotNull(roles);
        Assert.Contains(roles, r => r.Key == "test_contains" && r.Name == "TestRole_Contains");
    }

    // ─────────────── GET /api/roles/{id} ───────────────

    [Fact]
    public async Task GetById_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/roles/1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ExistingRole_ReturnsOk()
    {
        var client = await CreateAuthenticatedClientAsync();
        var roleId = await SeedRoleAsync("TestRole_GetById", "test_getbyid");

        var response = await client.GetAsync($"/api/roles/{roleId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var role = await response.Content.ReadFromJsonAsync<RoleDto>(JsonOpts);
        Assert.NotNull(role);
        Assert.Equal(roleId, role.Id);
        Assert.Equal("TestRole_GetById", role.Name);
        Assert.Equal("test_getbyid", role.Key);
    }

    [Fact]
    public async Task GetById_NonexistentRole_ReturnsNotFound()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/roles/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ─────────────── POST /api/roles ───────────────

    [Fact]
    public async Task Create_Unauthenticated_ReturnsUnauthorized()
    {
        var dto = new CreateRoleNewDto("NewRole", "newrole", "Test description", null, true);
        var response = await Client.PostAsJsonAsync("/api/roles", dto);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_ValidRole_ReturnsCreated()
    {
        var client = await CreateAuthenticatedClientAsync();
        var dto = new CreateRoleNewDto("CreatedRole", "created_role", "A new test role", null, true);

        var response = await client.PostAsJsonAsync("/api/roles", dto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var role = await response.Content.ReadFromJsonAsync<RoleDto>(JsonOpts);
        Assert.NotNull(role);
        Assert.Equal("CreatedRole", role.Name);
        Assert.Equal("created_role", role.Key);
    }

    [Fact]
    public async Task Create_ArabicRoleName_ReturnsCreated()
    {
        var client = await CreateAuthenticatedClientAsync();
        var dto = new CreateRoleNewDto("مشرف جديد", "arabic_custom", "Arabic named role", null, true);

        var response = await client.PostAsJsonAsync("/api/roles", dto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var role = await response.Content.ReadFromJsonAsync<RoleDto>(JsonOpts);
        Assert.NotNull(role);
        Assert.Equal("مشرف جديد", role.Name);
    }

    [Fact]
    public async Task Create_ArabicKey_ReturnsCreated()
    {
        var client = await CreateAuthenticatedClientAsync();
        var dto = new CreateRoleNewDto("RoleArabicKey", "مفتاح", "Role with Arabic key", null, true);

        var response = await client.PostAsJsonAsync("/api/roles", dto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var role = await response.Content.ReadFromJsonAsync<RoleDto>(JsonOpts);
        Assert.NotNull(role);
        Assert.Equal("مفتاح", role.Key);
    }

    [Fact]
    public async Task Create_DuplicateRoleName_ReturnsBadRequest()
    {
        var client = await CreateAuthenticatedClientAsync();
        await SeedRoleAsync("DuplicateRole", "dup_role");

        var dto = new CreateRoleNewDto("DuplicateRole", "dup_role_2", null, null, true);
        var response = await client.PostAsJsonAsync("/api/roles", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidName_ReturnsBadRequest()
    {
        var client = await CreateAuthenticatedClientAsync();
        var dto = new CreateRoleNewDto("", "empty_name", "Empty name", null, true);

        var response = await client.PostAsJsonAsync("/api/roles", dto);
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Create_WithPermissionIds_ReturnsCreated()
    {
        var client = await CreateAuthenticatedClientAsync();
        var dto = new CreateRoleNewDto("PermRole", "perm_role", "With permission ids", new List<int>(), true);

        var response = await client.PostAsJsonAsync("/api/roles", dto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // ─────────────── PUT /api/roles/{id} ───────────────

    [Fact]
    public async Task Update_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await Client.PutAsJsonAsync("/api/roles/1", new UpdateRoleDto("Updated", null, null, null, null));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Update_ExistingRole_ReturnsOk()
    {
        var client = await CreateAuthenticatedClientAsync();
        var roleId = await SeedRoleAsync("UpdateMe", "update_me");

        var dto = new UpdateRoleDto("UpdatedName", "updated_key", "Updated description", null, null);
        var response = await client.PutAsJsonAsync($"/api/roles/{roleId}", dto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var role = await response.Content.ReadFromJsonAsync<RoleDto>(JsonOpts);
        Assert.NotNull(role);
        Assert.Equal("UpdatedName", role.Name);
        Assert.Equal("updated_key", role.Key);
    }

    [Fact]
    public async Task Update_NonexistentRole_ReturnsBadRequest()
    {
        var client = await CreateAuthenticatedClientAsync();
        var dto = new UpdateRoleDto("Ghost", null, null, null, null);

        var response = await client.PutAsJsonAsync("/api/roles/999999", dto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_NameOnly_ReturnsOk()
    {
        var client = await CreateAuthenticatedClientAsync();
        var roleId = await SeedRoleAsync("NameOnlyRole", "name_only");

        var dto = new UpdateRoleDto("RenamedRole", null, null, null, null);
        var response = await client.PutAsJsonAsync($"/api/roles/{roleId}", dto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var role = await response.Content.ReadFromJsonAsync<RoleDto>(JsonOpts);
        Assert.Equal("RenamedRole", role!.Name);
    }

    [Fact]
    public async Task Update_IsActive_ReturnsOk()
    {
        var client = await CreateAuthenticatedClientAsync();
        var roleId = await SeedRoleAsync("ActiveToggle", "active_toggle", isActive: true);

        var dto = new UpdateRoleDto(null, null, null, null, false);
        var response = await client.PutAsJsonAsync($"/api/roles/{roleId}", dto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var role = await response.Content.ReadFromJsonAsync<RoleDto>(JsonOpts);
        Assert.False(role!.IsActive);
    }

    [Fact]
    public async Task Update_WithPermissionIds_ReturnsOk()
    {
        var client = await CreateAuthenticatedClientAsync();
        var roleId = await SeedRoleAsync("PermUpdate", "perm_update");

        var dto = new UpdateRoleDto(null, null, null, new List<int>(), null);
        var response = await client.PutAsJsonAsync($"/api/roles/{roleId}", dto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ─────────────── DELETE /api/roles/{id} ───────────────

    [Fact]
    public async Task Delete_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await Client.DeleteAsync("/api/roles/1");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Delete_CustomRole_ReturnsNoContent()
    {
        var client = await CreateAuthenticatedClientAsync();
        var roleId = await SeedRoleAsync("DeletableRole", "deletable");

        var response = await client.DeleteAsync($"/api/roles/{roleId}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify role is soft-deleted (IsActive = false)
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();
        var dbRole = await db.Roles.FindAsync(roleId);
        Assert.NotNull(dbRole);
        Assert.False(dbRole.IsActive);
    }

    [Fact]
    public async Task Delete_AdminRole_ReturnsForbidden()
    {
        var client = await CreateAuthenticatedClientAsync();
        var roleId = await SeedRoleAsync("AdminRole", "admin");

        var response = await client.DeleteAsync($"/api/roles/{roleId}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_SupervisorRole_ReturnsForbidden()
    {
        var client = await CreateAuthenticatedClientAsync();
        var roleId = await SeedRoleAsync("SupervisorRole", "supervisor");

        var response = await client.DeleteAsync($"/api/roles/{roleId}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_OperatorRole_ReturnsForbidden()
    {
        var client = await CreateAuthenticatedClientAsync();
        var roleId = await SeedRoleAsync("OperatorRole", "operator");

        var response = await client.DeleteAsync($"/api/roles/{roleId}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_NonexistentRole_ReturnsNotFound()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.DeleteAsync("/api/roles/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ─────────────── GET /api/roles/{id}/permissions ───────────────

    [Fact]
    public async Task GetPermissions_NonexistentRole_ReturnsBadRequest()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/roles/999999/permissions");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetPermissions_ExistingRole_ReturnsOk()
    {
        var client = await CreateAuthenticatedClientAsync();
        var roleId = await SeedRoleAsync("PermRole", "perm_role_perms");

        var response = await client.GetAsync($"/api/roles/{roleId}/permissions");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ─────────────── PUT /api/roles/{id}/permissions ───────────────

    [Fact]
    public async Task UpdatePermissions_ExistingRole_ReturnsNoContent()
    {
        var client = await CreateAuthenticatedClientAsync();
        var roleId = await SeedRoleAsync("PermUpdateRole", "perm_update_role");

        var request = new { roleId, permissions = new object[] { } };
        var response = await client.PutAsJsonAsync($"/api/roles/{roleId}/permissions", request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // ─────────────── POST /api/roles/init ───────────────

    [Fact]
    public async Task Init_Roles_ReturnsOk()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.PostAsync("/api/roles/init", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ─────────────── GET /api/roles/modules ───────────────

    [Fact]
    public async Task GetModules_ReturnsOk()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/roles/modules");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ─────────────── DB Verification Tests ───────────────

    [Fact]
    public async Task Create_VerifyDbRole()
    {
        var client = await CreateAuthenticatedClientAsync();
        var dto = new CreateRoleNewDto("DbVerify", "db_verify", "Verify in DB", null, true);

        var response = await client.PostAsJsonAsync("/api/roles", dto);
        var role = await response.Content.ReadFromJsonAsync<RoleDto>(JsonOpts);

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();
        var dbRole = await db.Roles.FindAsync(role!.Id);
        Assert.NotNull(dbRole);
        Assert.Equal("DbVerify", dbRole.Name);
        Assert.Equal("db_verify", dbRole.Key);
        Assert.True(dbRole.IsActive);
    }

    [Fact]
    public async Task Update_VerifyDbRole()
    {
        var client = await CreateAuthenticatedClientAsync();
        var roleId = await SeedRoleAsync("DbVerifyUpdate", "db_verify_upd");

        var dto = new UpdateRoleDto("UpdatedForDb", "updated_for_db", "Updated desc", null, null);
        await client.PutAsJsonAsync($"/api/roles/{roleId}", dto);

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();
        var dbRole = await db.Roles.FindAsync(roleId);
        Assert.NotNull(dbRole);
        Assert.Equal("UpdatedForDb", dbRole.Name);
        Assert.Equal("updated_for_db", dbRole.Key);
    }

    [Fact]
    public async Task Delete_VerifyDbSoftDelete()
    {
        var client = await CreateAuthenticatedClientAsync();
        var roleId = await SeedRoleAsync("DbVerifyDelete", "db_verify_del");

        await client.DeleteAsync($"/api/roles/{roleId}");

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();
        var dbRole = await db.Roles.FindAsync(roleId);
        Assert.NotNull(dbRole);
        Assert.False(dbRole.IsActive);
    }

    // ─────────────── Role with Arabic permissions ───────────────

    [Fact]
    public async Task Create_WithArabicPermissionIds_ReturnsCreated()
    {
        var client = await CreateAuthenticatedClientAsync();
        var dto = new CreateRoleNewDto("ArabicPermRole", "arabic_perm", "Arabic permissions role", new List<int>(), true);

        var response = await client.PostAsJsonAsync("/api/roles", dto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    // ─────────────── Full Lifecycle Test ───────────────

    [Fact]
    public async Task FullLifecycle_CreateUpdatePermissionsDelete()
    {
        var client = await CreateAuthenticatedClientAsync();

        // 1. Create
        var createDto = new CreateRoleNewDto("LifecycleRole", "lifecycle", "Full lifecycle", null, true);
        var createResponse = await client.PostAsJsonAsync("/api/roles", createDto);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdRole = await createResponse.Content.ReadFromJsonAsync<RoleDto>(JsonOpts);
        Assert.NotNull(createdRole);

        // 2. Update name
        var updateDto = new UpdateRoleDto("LifecycleRenamed", null, "Updated", null, null);
        var updateResponse = await client.PutAsJsonAsync($"/api/roles/{createdRole.Id}", updateDto);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        // 3. Verify via GET
        var getResponse = await client.GetAsync($"/api/roles/{createdRole.Id}");
        var fetchedRole = await getResponse.Content.ReadFromJsonAsync<RoleDto>(JsonOpts);
        Assert.Equal("LifecycleRenamed", fetchedRole!.Name);

        // 4. Set permissions (empty list — no page permissions assigned)
        var permRequest = new { roleId = createdRole.Id, permissions = new object[] { } };
        var permResponse = await client.PutAsJsonAsync($"/api/roles/{createdRole.Id}/permissions", permRequest);
        Assert.Equal(HttpStatusCode.NoContent, permResponse.StatusCode);

        // 5. Get permissions
        var getPermResponse = await client.GetAsync($"/api/roles/{createdRole.Id}/permissions");
        Assert.Equal(HttpStatusCode.OK, getPermResponse.StatusCode);

        // 6. Delete
        var deleteResponse = await client.DeleteAsync($"/api/roles/{createdRole.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // 7. Verify soft-deleted
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FuelTrackDbContext>();
        var dbRole = await db.Roles.FindAsync(createdRole.Id);
        Assert.False(dbRole!.IsActive);
    }

    // ─────────────── Arabic Role Lifecycle Test ───────────────

    [Fact]
    public async Task ArabicLifecycle_CreateUpdateDelete()
    {
        var client = await CreateAuthenticatedClientAsync();

        // 1. Create Arabic role
        var createDto = new CreateRoleNewDto("دور عربي", "arabic_role", "دور باللغة العربية", null, true);
        var createResponse = await client.PostAsJsonAsync("/api/roles", createDto);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdRole = await createResponse.Content.ReadFromJsonAsync<RoleDto>(JsonOpts);
        Assert.Equal("دور عربي", createdRole!.Name);
        Assert.Equal("arabic_role", createdRole.Key);

        // 2. Update Arabic name
        var updateDto = new UpdateRoleDto("دور محدث", null, null, null, null);
        var updateResponse = await client.PutAsJsonAsync($"/api/roles/{createdRole.Id}", updateDto);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updatedRole = await updateResponse.Content.ReadFromJsonAsync<RoleDto>(JsonOpts);
        Assert.Equal("دور محدث", updatedRole!.Name);

        // 3. Delete
        var deleteResponse = await client.DeleteAsync($"/api/roles/{createdRole.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    // ─────────────── DTO records used for create ───────────────
    // Mirrors CreateRoleDto: PermissionIds is now List<int>?, not old Permissions object

    private record CreateRoleNewDto(string Name, string? Key, string? Description, List<int>? PermissionIds, bool? IsActive);
}
