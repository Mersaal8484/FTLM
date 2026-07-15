# Template: Unit Test

## Metadata
- **Name**: unit-test
- **Purpose**: xUnit unit test for service or controller
- **Layer**: Tests
- **Path**: `tests/FuelTrack.Tests/Unit/{Service}/{Name}Tests.cs`

## Template

```csharp
using FuelTrack.Application.DTOs.{Entity};
using FuelTrack.Application.Interfaces;
using FuelTrack.Infrastructure.Services;
using FuelTrack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FuelTrack.Tests.Unit.{Service};

public class {Name}ServiceTests
{
    private readonly Mock<FuelTrackDbContext> _mockContext;
    private readonly Mock<ILogger<{Name}Service>> _mockLogger;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly {Name}Service _sut;

    public {Name}ServiceTests()
    {
        _mockContext = new Mock<FuelTrackDbContext>();
        _mockLogger = new Mock<ILogger<{Name}Service>>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _sut = new {Name}Service(
            _mockContext.Object,
            _mockLogger.Object,
            _mockHttpContextAccessor.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingEntity_ReturnsDto()
    {
        // Arrange
        var id = 1;
        var entity = new {Entity} { Id = id, Name = "Test" };
        var mockSet = new Mock<DbSet<{Entity}>>();
        mockSet.As<IQueryable<{Entity}>>()
            .Setup(m => m.Provider)
            .Returns(new[] { entity }.AsQueryable().Provider);
        _mockContext.Setup(c => c.{Entities}).Returns(mockSet.Object);

        // Act
        var result = await _sut.GetByIdAsync(id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal("Test", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingEntity_ReturnsNull()
    {
        // Arrange
        var mockSet = new Mock<DbSet<{Entity}>>();
        mockSet.As<IQueryable<{Entity}>>()
            .Setup(m => m.Provider)
            .Returns(new {Entity}[0].AsQueryable().Provider);
        _mockContext.Setup(c => c.{Entities}).Returns(mockSet.Object);

        // Act
        var result = await _sut.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var dto = new Create{Entity}Dto { Name = "New Entity" };
        var httpContext = new DefaultHttpContext();
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Entity", result.Name);
    }
}
```

## Checklist
- [ ] Uses xUnit `[Fact]` or `[Theory]`
- [ ] Arrange-Act-Assert pattern
- [ ] Mock setup for all dependencies
- [ ] Tests both happy path and error cases
- [ ] All entity types use `int` (not `Guid`)
- [ ] Test data matches current domain models
- [ ] No test interdependencies
- [ ] Test names follow `Method_Scenario_ExpectedResult` pattern
