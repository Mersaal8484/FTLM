namespace FuelTrack.Application.Interfaces;

public interface IUserScopeFilterService
{
    Task<List<int>> GetAccessibleSubStationIdsAsync(int userId);
    Task<List<int>> GetAccessibleStationIdsAsync(int userId);
    Task<List<int>> GetAccessibleBranchIdsAsync(int userId);
    Task<List<int>> GetAccessibleMeterIdsAsync(int userId);
    Task<List<int>> GetUserIdsWithAccessToSubStationAsync(int subStationId);
    Task<bool> HasAccessToSubStationAsync(int userId, int subStationId);
}
