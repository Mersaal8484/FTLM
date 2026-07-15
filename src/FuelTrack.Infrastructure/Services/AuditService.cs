using System.Globalization;
using System.Security.Claims;
using System.Text;
using FuelTrack.Application;
using FuelTrack.Application.DTOs.Audits;
using FuelTrack.Application.Interfaces;
using FuelTrack.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace FuelTrack.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly FuelTrackDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(FuelTrackDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    private int? GetCurrentUserId()
    {
        var claim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
    }

    private bool IsAdmin()
    {
        return _httpContextAccessor.HttpContext?.User.IsInRole("Admin") ?? false;
    }

    public async Task<PagedResult<AuditListDto>> GetPagedAsync(AuditFilterParams filter)
    {
        var query = _db.AuditLogs
            .Where(x => x.IsActive)
            .AsQueryable();

        if (!IsAdmin())
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId.HasValue)
            {
                query = query.Where(x => x.UserId == currentUserId.Value);
            }
        }

        if (filter.StartDate.HasValue)
            query = query.Where(x => x.Timestamp >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(x => x.Timestamp <= filter.EndDate.Value);

        if (filter.UserId.HasValue)
            query = query.Where(x => x.UserId == filter.UserId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Action))
            query = query.Where(x => x.Action.Contains(filter.Action));

        if (!string.IsNullOrWhiteSpace(filter.EntityName))
            query = query.Where(x => x.EntityName.Contains(filter.EntityName));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.Timestamp)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(x => new AuditListDto(
                x.Id,
                x.UserId,
                _db.Users.Where(u => u.Id == x.UserId).Select(u => u.FullName).FirstOrDefault() ?? "",
                x.Action,
                x.EntityName,
                x.EntityId.ToString(),
                null,
                null,
                x.OldValues,
                x.NewValues,
                x.IpAddress,
                x.Timestamp
            ))
            .ToListAsync();

        return new PagedResult<AuditListDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<byte[]> ExportCsvAsync(AuditFilterParams filter)
    {
        var data = await GetPagedAsync(filter with { Page = 1, PageSize = int.MaxValue });

        var sb = new StringBuilder();
        sb.AppendLine("Usuário;Ação;Entidade;Id Entidade;Ip;Timestamp");

        foreach (var item in data.Items)
        {
            sb.AppendLine(
                $"{EscapeCsv(item.UserName)};{EscapeCsv(item.Action)};{EscapeCsv(item.EntityName)};{EscapeCsv(item.EntityId)};{EscapeCsv(item.IpAddress)};{item.Timestamp:yyyy-MM-dd HH:mm:ss}");
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(';') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
