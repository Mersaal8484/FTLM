using System.Threading.Tasks;
using FuelTrack.Application.DTOs.Compliance;

namespace FuelTrack.Application.Interfaces
{
    public interface IComplianceService
    {
        Task<UserDataExportDto?> ExportUserDataAsync(int userId, string requestedBy);
        Task<AnonymizeUserResultDto> AnonymizeUserPersonalDataAsync(int userId, string requestedBy);
        Task<ComplianceAuditSummaryDto> GetComplianceAuditSummaryAsync();
    }
}
