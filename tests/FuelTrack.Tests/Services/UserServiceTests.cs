using FuelTrack.Application.DTOs.RBAC;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace FuelTrack.Tests.Services;

public class UserServiceTests : IDisposable
{
    private readonly FuelTrackDbContext _db;
    private readonly UserManager<User> _userManager;
    private readonly UserService _service;

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase(databaseName: $"UserTestDb_{Guid.NewGuid()}")
            .Options;
        _db = new FuelTrackDbContext(options);

        var store = new Microsoft.AspNetCore.Identity.EntityFrameworkCore.UserStore<User, Role, FuelTrackDbContext, int>(_db);
        var passwordHasher = new PasswordHasher<User>();
        _userManager = new UserManager<User>(store, null, passwordHasher, null, null, null, null, null, null);
        
        _service = new UserService(_userManager, _db);
    }

    [Fact]
    public async Task CreateAsync_ValidInput_CreatesUser()
    {
        // Arrange
        var dto = new CreateUserDto(
            "testuser",
            "Test User",
            "test@example.com",
            "123456789",
            "Password123!",
            null,
            null
        );

        // Act
        var result = await _service.CreateAsync(dto, 1);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal(dto.UserName, result.Value!.UserName);
        Assert.Equal(dto.Email, result.Value.Email);
        Assert.Equal(dto.FullName, result.Value.FullName);
        
        var userInDb = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        Assert.NotNull(userInDb);
        Assert.Equal(dto.UserName, userInDb.UserName);
    }

    [Fact]
    public async Task UpdateAsync_ValidInput_UpdatesUser()
    {
        // Arrange
        var user = new User { UserName = "updateuser", Email = "old@example.com", FullName = "Old Name", IsActive = true };
        await _userManager.CreateAsync(user, "Password123!");

        var dto = new UpdateUserDto(
            "New Name",
            "new@example.com",
            "987654321",
            false
        );

        // Act
        var result = await _service.UpdateAsync(user.Id, dto, 1);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("New Name", result.Value!.FullName);
        Assert.Equal("new@example.com", result.Value.Email);
        Assert.False(result.Value.IsActive);
        
        var userInDb = await _userManager.FindByIdAsync(user.Id.ToString());
        Assert.Equal("New Name", userInDb!.FullName);
    }

    [Fact]
    public async Task DeleteAsync_SetsIsActiveToFalse()
    {
        // Arrange
        var user = new User { UserName = "deleteuser", Email = "del@example.com", IsActive = true };
        await _userManager.CreateAsync(user, "Password123!");

        // Act
        await _service.DeleteAsync(user.Id);

        // Assert
        var userInDb = await _userManager.FindByIdAsync(user.Id.ToString());
        Assert.False(userInDb!.IsActive);
    }
    
    [Fact]
    public async Task GetPagedAsync_ReturnsCorrectPagination()
    {
        // Arrange
        for(int i=0; i<15; i++)
        {
            await _userManager.CreateAsync(new User { UserName = $"user{i}", Email = $"user{i}@example.com" }, "Password123!");
        }

        // Act
        var page1 = await _service.GetPagedAsync(1, 10);
        var page2 = await _service.GetPagedAsync(2, 10);

        // Assert
        Assert.Equal(15, page1.TotalCount);
        Assert.Equal(10, page1.Items.Count);
        Assert.Equal(5, page2.Items.Count);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
        _userManager.Dispose();
    }
}
