using FuelTrack.Application.Interfaces;

namespace FuelTrack.Tests.Services;

public class StubUserScopeFilterService : IUserScopeFilterService
{
    public Task<List<int>> GetAccessibleSubStationIdsAsync(int userId)
        => Task.FromResult(new List<int>());

    public Task<List<int>> GetAccessibleStationIdsAsync(int userId)
        => Task.FromResult(new List<int>());

    public Task<List<int>> GetAccessibleBranchIdsAsync(int userId)
        => Task.FromResult(new List<int>());

    public Task<bool> HasAccessToSubStationAsync(int userId, int subStationId)
        => Task.FromResult(true);
}
