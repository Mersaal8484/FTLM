# Template: Integration Test

## Metadata
- **Name**: integration-test
- **Purpose**: Integration test for API endpoints
- **Layer**: Tests
- **Path**: `tests/FuelTrack.Tests/Integration/{Name}IntegrationTests.cs`

## Template

```csharp
using FuelTrack.API;
using FuelTrack.Application.DTOs.{Entity};
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace FuelTrack.Tests.Integration;

public class {Name}IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public {Name}IntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/{entities}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_Existing_ReturnsOk()
    {
        // Arrange
        var id = 1;

        // Act
        var response = await _client.GetAsync($"/api/{entities}/{id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_NonExisting_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/{entities}/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_Unauthorized_ReturnsUnauthorized()
    {
        // Arrange
        var dto = new Create{Entity}Dto { Name = "Test" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/{entities}", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
```

## Checklist
- [ ] Uses `WebApplicationFactory<Program>`
- [ ] Tests HTTP status codes
- [ ] Tests authentication/authorization
- [ ] Tests CRUD operations end-to-end
- [ ] Tests validation errors
- [ ] Tests not found scenarios
- [ ] Uses `IClassFixture` for shared factory
- [ ] Test data cleaned up after tests
