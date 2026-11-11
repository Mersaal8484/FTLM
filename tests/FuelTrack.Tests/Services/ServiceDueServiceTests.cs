using FuelTrack.Application.DTOs.Maintenance;
using FuelTrack.Application.DTOs.Notifications;
using FuelTrack.Application.Interfaces;
using FuelTrack.Domain.Enums;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FuelTrack.Tests.Services;

public sealed class ServiceDueServiceTests : IDisposable
{
    private readonly FuelTrackDbContext _db;
    private readonly Mock<INotificationService> _notifications = new();
    private readonly ServiceDueEvaluationService _dueEvaluation;
    private readonly ServiceReplacementService _replacementService;
    private readonly ServiceDueService _dueService;
    private readonly AlertGeneratorService _alertGeneratorService;
    private readonly StubCurrentUserService _currentUser = new() { IsAdmin = true, UserId = 1 };
    private readonly StubUserScopeFilterService _scopeFilter = new();

    public ServiceDueServiceTests()
    {
        _db = new FuelTrackDbContext(new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase($"ServiceDueTests_{Guid.NewGuid()}").Options);

        _dueEvaluation = new ServiceDueEvaluationService(_db, _notifications.Object, NullLogger<ServiceDueEvaluationService>.Instance);
        _replacementService = new ServiceReplacementService(_db, _dueEvaluation, _scopeFilter, _currentUser, new IdempotencyService(_db), NullLogger<ServiceReplacementService>.Instance);
        _dueService = new ServiceDueService(_db, _scopeFilter, _currentUser);
        _alertGeneratorService = new AlertGeneratorService(_db, _notifications.Object);
    }

    [Fact]
    public async Task CheckInstalledPartsReplacementAsync_GatedWhenOperationalDataEnabled()
    {
        // 1. Enable operational_data feature flag
        _db.CustomerFeatureSettings.Add(new CustomerFeatureSetting
        {
            Key = "operational_data",
            IsEnabled = true,
            Value = CustomerFeatureService.NewModeValue,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        // 2. Add an installed part that would otherwise trigger legacy overdue alert
        var subStation = new SubStation { Id = 1, Name = "SS1", Code = "SS1", StationId = 1 };
        var controller = new Controller { Id = 1, OperatingHours = 6000 };
        var generator = new Generator { Id = 10, Name = "Gen1", SubStationId = 1, SubStation = subStation, Controller = controller };
        var sparePart = new SparePart { Id = 20, Name = "Oil Filter" };

        _db.SubStations.Add(subStation);
        _db.Controllers.Add(controller);
        _db.Generators.Add(generator);
        _db.SpareParts.Add(sparePart);
        _db.InstalledParts.Add(new InstalledPart
        {
            Id = 30,
            GeneratorId = 10,
            Generator = generator,
            SparePartId = 20,
            SparePart = sparePart,
            IsActive = true,
            EstimatedReplacementAtHours = 5500 // Operating hours 6000 > 5500 (overdue)
        });
        await _db.SaveChangesAsync();

        // 3. Check alerts: must return 0 because legacy alert is suppressed!
        var created = await _alertGeneratorService.CheckInstalledPartsReplacementAsync();
        Assert.Equal(0, created);
        _notifications.Verify(x => x.PublishScopedAsync(It.IsAny<CreateNotificationDto>(), null, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecordReplacement_And_DueService_FullLifecycle()
    {
        _db.CustomerFeatureSettings.Add(new CustomerFeatureSetting
        {
            Key = "operational_data",
            IsEnabled = true,
            Value = CustomerFeatureService.NewModeValue,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        // Setup generator and service items
        var subStation = new SubStation { Id = 1, Name = "North Station", Code = "NS", StationId = 1 };
        var generator = new Generator
        {
            Id = 100,
            Name = "G-01",
            Model = "Perkins 4012",
            SubStationId = 1,
            SubStation = subStation
        };
        var serviceItem = new ServiceItem
        {
            Id = 1,
            Code = "OilChangeSet",
            NameAr = "بدلة زيت",
            NameEn = "Oil change set",
            SortOrder = 1,
            SuggestedIntervalHours = 250m,
            IsActive = true
        };
        var definition = new ServiceIntervalDefinition
        {
            Id = 1,
            GeneratorModelKey = "PERKINS 4012",
            ServiceItemId = 1,
            ServiceItem = serviceItem,
            IntervalHours = 250m,
            WarningLeadHours = 50m,
            Importance = ServiceIntervalImportance.Critical
        };

        _db.SubStations.Add(subStation);
        _db.Generators.Add(generator);
        _db.ServiceItems.Add(serviceItem);
        _db.ServiceIntervalDefinitions.Add(definition);
        await _db.SaveChangesAsync();

        // 1. Initial state: no readings, no baseline -> NotBaselined
        var initialTable = await _dueService.GetGeneratorMaintenanceTableAsync(100);
        Assert.True(initialTable.IsSuccess);
        Assert.Equal(ServiceDueStatus.NotBaselined, initialTable.Value!.Items[0].Status);

        // 2. Record initial baseline at 1000 h
        var baselineRes = await _replacementService.RecordReplacementAsync(new RecordReplacementRequestDto(
            GeneratorId: 100,
            ServiceItemId: 1,
            HoursAtService: 1000m,
            OccurredAtUtc: DateTime.UtcNow.AddDays(-10),
            Notes: "Initial baseline", ClientRequestId: Guid.NewGuid()), 1);
        Assert.True(baselineRes.IsSuccess);

        // 3. Add approved daily closing at 1100 h (100 h elapsed of 250 h interval)
        _db.MeterReadings.Add(new MeterReading
        {
            Id = 501,
            SubjectType = MeterReadingSubjectType.GeneratorRunningHours,
            SubjectId = 100,
            Kind = MeterReadingKind.DailyClosing,
            Status = OperationalReadingStatus.Approved,
            RawValue = 1100m,
            MultiplierSnapshot = 1m,
            CapturedAtUtc = DateTime.UtcNow.AddDays(-5),
            BusinessDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5))
        });
        await _db.SaveChangesAsync();

        // 4. Check due table: 1100 h -> remaining = 250 - (1100 - 1000) = 150 h (> 50 lead) -> Ok
        var okTable = await _dueService.GetGeneratorMaintenanceTableAsync(100);
        Assert.True(okTable.IsSuccess);
        var item1 = okTable.Value!.Items[0];
        Assert.Equal(ServiceDueStatus.Ok, item1.Status);
        Assert.Equal(150m, item1.RemainingHours);
        Assert.Equal(1250m, item1.DueAtHours);

        // 5. Advanced reading: 1210 h -> remaining = 40 h (<= 50 lead) -> DueSoon!
        _db.MeterReadings.Add(new MeterReading
        {
            Id = 502,
            SubjectType = MeterReadingSubjectType.GeneratorRunningHours,
            SubjectId = 100,
            Kind = MeterReadingKind.DailyClosing,
            Status = OperationalReadingStatus.Approved,
            RawValue = 1210m,
            MultiplierSnapshot = 1m,
            CapturedAtUtc = DateTime.UtcNow.AddDays(-1),
            BusinessDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))
        });
        await _db.SaveChangesAsync();

        var dueSoonTable = await _dueService.GetGeneratorMaintenanceTableAsync(100);
        Assert.Equal(ServiceDueStatus.DueSoon, dueSoonTable.Value!.Items[0].Status);
        Assert.Equal(40m, dueSoonTable.Value!.Items[0].RemainingHours);

        // 6. Evaluate alerts: must publish DueSoon notification
        _notifications.Setup(x => x.PublishScopedAsync(It.IsAny<CreateNotificationDto>(), null, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationDto(1, 1, "", "", "service_due_soon", "warning", false, "Generator", 100, null, null, DateTime.UtcNow));

        var alertCount1 = await _dueEvaluation.EvaluateGeneratorAsync(100);
        Assert.Equal(1, alertCount1);

        // 7. Deduplication: running evaluation again with same status -> 0 new notifications
        var alertCount2 = await _dueEvaluation.EvaluateGeneratorAsync(100);
        Assert.Equal(0, alertCount2);

        // 8. Advanced reading: 1260 h -> Overdue by 10 h!
        _db.MeterReadings.Add(new MeterReading
        {
            Id = 503,
            SubjectType = MeterReadingSubjectType.GeneratorRunningHours,
            SubjectId = 100,
            Kind = MeterReadingKind.DailyClosing,
            Status = OperationalReadingStatus.Approved,
            RawValue = 1260m,
            MultiplierSnapshot = 1m,
            CapturedAtUtc = DateTime.UtcNow,
            BusinessDate = DateOnly.FromDateTime(DateTime.UtcNow)
        });
        await _db.SaveChangesAsync();

        // 9. Evaluate alerts: upward escalation from DueSoon to Overdue -> publishes alert!
        var alertCount3 = await _dueEvaluation.EvaluateGeneratorAsync(100);
        Assert.Equal(1, alertCount3);

        // 10. Perform replacement at 1260 h -> clock restarts, returns to Ok!
        var repRes = await _replacementService.RecordReplacementAsync(new RecordReplacementRequestDto(
            GeneratorId: 100,
            ServiceItemId: 1,
            HoursAtService: 1260m,
            OccurredAtUtc: DateTime.UtcNow,
            Notes: "Replaced oil set", ClientRequestId: Guid.NewGuid()), 1);
        Assert.True(repRes.IsSuccess);

        var refreshedTable = await _dueService.GetGeneratorMaintenanceTableAsync(100);
        Assert.Equal(ServiceDueStatus.Ok, refreshedTable.Value!.Items[0].Status);
        Assert.Equal(250m, refreshedTable.Value!.Items[0].RemainingHours);
        Assert.Equal(1510m, refreshedTable.Value!.Items[0].DueAtHours);
    }

    [Fact]
    public async Task BulkBaseline_CreatesMultipleRecords_AndEvaluates()
    {
        var subStation = new SubStation { Id = 1, Name = "SS1", Code = "SS1", StationId = 1 };
        var generator = new Generator { Id = 200, Name = "G-02", Model = "CAT-3516B", SubStationId = 1, SubStation = subStation };
        var item1 = new ServiceItem { Id = 1, Code = "OilChangeSet", NameAr = "بدلة زيت", NameEn = "Oil change set", SuggestedIntervalHours = 250m };
        var item2 = new ServiceItem { Id = 2, Code = "AirFilter", NameAr = "فلتر هواء", NameEn = "Air filter", SuggestedIntervalHours = 1000m };

        _db.SubStations.Add(subStation);
        _db.Generators.Add(generator);
        _db.ServiceItems.AddRange(item1, item2);
        _db.ServiceIntervalDefinitions.AddRange(
            new ServiceIntervalDefinition { GeneratorModelKey = "CAT-3516B", ServiceItemId = 1, ServiceItem = item1, IntervalHours = 250m },
            new ServiceIntervalDefinition { GeneratorModelKey = "CAT-3516B", ServiceItemId = 2, ServiceItem = item2, IntervalHours = 1000m });
        await _db.SaveChangesAsync();

        var result = await _replacementService.RecordBulkBaselineAsync(new BulkBaselineRequestDto(
            GeneratorId: 200,
            Items: new List<BulkBaselineRowDto>
            {
                new(1, 4500m, DateTime.UtcNow, "Bulk baseline item 1"),
                new(2, 4500m, DateTime.UtcNow, "Bulk baseline item 2")
            }, ClientRequestId: Guid.NewGuid()), 1);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
        Assert.All(result.Value, r => Assert.Equal(ServiceRecordKind.Baseline, r.Kind));
    }

    [Fact]
    public async Task Void_MarksRecordVoided_AndRestoresPreviousHours()
    {
        var subStation = new SubStation { Id = 1, Name = "SS1", Code = "SS1", StationId = 1 };
        var generator = new Generator { Id = 300, Name = "G-03", Model = "Perkins 4012", SubStationId = 1, SubStation = subStation };
        var item = new ServiceItem { Id = 1, Code = "OilChangeSet", NameAr = "بدلة زيت", NameEn = "Oil change set", SuggestedIntervalHours = 250m };

        _db.SubStations.Add(subStation);
        _db.Generators.Add(generator);
        _db.ServiceItems.Add(item);
        _db.ServiceIntervalDefinitions.Add(new ServiceIntervalDefinition
        {
            GeneratorModelKey = "PERKINS 4012",
            ServiceItemId = item.Id,
            ServiceItem = item,
            IntervalHours = 250m
        });
        await _db.SaveChangesAsync();

        // 1. Initial valid baseline at 1000 h
        var rec1 = await _replacementService.RecordReplacementAsync(new RecordReplacementRequestDto(
            GeneratorId: 300, ServiceItemId: 1, HoursAtService: 1000m, OccurredAtUtc: DateTime.UtcNow.AddDays(-5), ClientRequestId: Guid.NewGuid()), 1);

        // 2. Erroneous second record at 1050 h
        var rec2 = await _replacementService.RecordReplacementAsync(new RecordReplacementRequestDto(
            GeneratorId: 300, ServiceItemId: 1, HoursAtService: 1050m, OccurredAtUtc: DateTime.UtcNow.AddDays(-2),
            EarlyReplacementReason: "Mistaken entry", ClientRequestId: Guid.NewGuid()), 1);

        // 3. Void rec2
        var voidRes = await _replacementService.VoidAsync(rec2.Value!.Id, new VoidReplacementRecordDto("Data entry mistake"), 1);
        Assert.True(voidRes.IsSuccess);

        // 4. Verify in DB
        var storedRec2 = await _db.ServiceReplacementRecords.FindAsync(rec2.Value.Id);
        Assert.NotNull(storedRec2!.VoidedAtUtc);
        Assert.Equal("Data entry mistake", storedRec2.VoidReason);

        // 5. Active record is rec1 again
        var history = await _replacementService.GetHistoryAsync(300, 1);
        Assert.Equal(2, history.Count);
        var activeRecord = history.FirstOrDefault(h => h.VoidedAtUtc == null);
        Assert.NotNull(activeRecord);
        Assert.Equal(1000m, activeRecord!.HoursAtService);
    }

    [Fact]
    public async Task ServiceDue_DeniesGeneratorOutsideCurrentUserScope()
    {
        var station = new Station { Id = 91, Name = "Station91", Code = "ST91" };
        var site = new SubStation { Id = 91, Name = "BlockedSite", Code = "BS", StationId = 91, Station = station };
        var generator = new Generator { Id = 901, Name = "Blocked", Model = "X", SubStationId = 91, SubStation = site };
        _db.Stations.Add(station);
        _db.SubStations.Add(site);
        _db.Generators.Add(generator);
        await _db.SaveChangesAsync();

        var user = new StubCurrentUserService { IsAdmin = false, UserId = 77 };
        var deniedScope = new DeniedScope();
        var service = new ServiceDueService(_db, deniedScope, user);

        var result = await service.GetGeneratorMaintenanceTableAsync(generator.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public async Task ServiceDue_DoesNotTreatSuggestedIntervalAsConfiguredDefinition()
    {
        var station = new Station { Id = 92, Name = "Station92", Code = "ST92" };
        var site = new SubStation { Id = 92, Name = "Unconfigured", Code = "UNC", StationId = 92, Station = station };
        var generator = new Generator { Id = 902, Name = "G-UNC", Model = "Unconfigured model", SubStationId = site.Id, SubStation = site };
        var item = new ServiceItem { Id = 92, Code = "SuggestedOnly", NameAr = "خدمة", NameEn = "Suggested only", SuggestedIntervalHours = 250m, IsActive = true };
        _db.AddRange(station, site, generator, item);
        await _db.SaveChangesAsync();

        var result = await _dueService.GetGeneratorMaintenanceTableAsync(generator.Id);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        var tableItem = result.Value.Items[0];
        Assert.Equal(ServiceDueStatus.NotConfigured, tableItem.Status);
        Assert.Equal(0m, tableItem.IntervalHours);
        Assert.NotNull(tableItem.DefinitionLink);
    }

    [Fact]
    public async Task ServiceReplacement_DeniesOperations_WhenOutsideScope()
    {
        var site = new SubStation { Id = 95, Name = "DeniedSite", Code = "DS", StationId = 1 };
        var gen = new Generator { Id = 950, Name = "G-Denied", Model = "CAT", SubStationId = 95, SubStation = site };
        var item = new ServiceItem { Id = 95, Code = "Item95", NameAr = "عنصر", NameEn = "Item 95", IsActive = true };
        _db.AddRange(site, gen, item);
        await _db.SaveChangesAsync();

        var deniedUser = new StubCurrentUserService { IsAdmin = false, UserId = 99 };
        var deniedScope = new DeniedScope();
        var deniedService = new ServiceReplacementService(_db, _dueEvaluation, deniedScope, deniedUser, new IdempotencyService(_db), NullLogger<ServiceReplacementService>.Instance);

        // 1. Record Replacement -> 403
        var repRes = await deniedService.RecordReplacementAsync(new RecordReplacementRequestDto(950, 95, 100m, DateTime.UtcNow, ClientRequestId: Guid.NewGuid()), 99);
        Assert.False(repRes.IsSuccess);
        Assert.Equal(403, repRes.StatusCode);

        // 2. Bulk baseline -> 403
        var bulkRes = await deniedService.RecordBulkBaselineAsync(new BulkBaselineRequestDto(950, new List<BulkBaselineRowDto> { new(95, 100m, DateTime.UtcNow) }, Guid.NewGuid()), 99);
        Assert.False(bulkRes.IsSuccess);
        Assert.Equal(403, bulkRes.StatusCode);

        // 3. History -> empty
        var history = await deniedService.GetHistoryAsync(950, 95);
        Assert.Empty(history);
    }

    [Fact]
    public async Task RecordReplacement_AllowsItemWithoutDefinition_AsUnconfiguredSnapshotNull()
    {
        var site = new SubStation { Id = 81, Name = "SS81", Code = "S81", StationId = 1 };
        var gen = new Generator { Id = 810, Name = "G-810", Model = "Perkins", SubStationId = 81, SubStation = site };
        var item = new ServiceItem { Id = 81, Code = "NoDefItem", NameAr = "بدون تعريف", NameEn = "No Def Item", IsActive = true };
        _db.AddRange(site, gen, item);
        await _db.SaveChangesAsync();

        var result = await _replacementService.RecordReplacementAsync(new RecordReplacementRequestDto(
            GeneratorId: 810,
            ServiceItemId: 81,
            HoursAtService: 1500m,
            OccurredAtUtc: DateTime.UtcNow,
            Notes: "Unconfigured item replacement",
            ClientRequestId: Guid.NewGuid()), 1);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.IntervalHoursSnapshot);
        Assert.False(result.Value.IsEarly);
    }

    [Fact]
    public async Task RecordMultiItemReplacement_AtomicallyRecordsMultipleItems()
    {
        var site = new SubStation { Id = 82, Name = "SS82", Code = "S82", StationId = 1 };
        var gen = new Generator { Id = 820, Name = "G-820", Model = "Perkins", SubStationId = 82, SubStation = site };
        var item1 = new ServiceItem { Id = 821, Code = "Item1", NameAr = "بند 1", NameEn = "Item 1", IsActive = true };
        var item2 = new ServiceItem { Id = 822, Code = "Item2", NameAr = "بند 2", NameEn = "Item 2", IsActive = true };
        _db.AddRange(site, gen, item1, item2);
        await _db.SaveChangesAsync();

        var clientReqId = Guid.NewGuid();
        var result = await _replacementService.RecordMultiItemReplacementAsync(new MultiItemReplacementRequestDto(
            GeneratorId: 820,
            HoursAtService: 2000m,
            OccurredAtUtc: DateTime.UtcNow,
            WorkPermitId: null,
            Items: new List<MultiItemReplacementRowDto>
            {
                new(821, "Notes for 821", null),
                new(822, "Notes for 822", null)
            },
            ClientRequestId: clientReqId), 1);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
        Assert.All(result.Value, r => Assert.Equal(2000m, r.HoursAtService));
    }

    [Fact]
    public async Task RecordReplacement_PreSaveValidationFailure_DoesNotPersistIdempotency()
    {
        var site = new SubStation { Id = 83, Name = "SS83", Code = "S83", StationId = 1 };
        var gen = new Generator { Id = 830, Name = "G-830", Model = "Perkins", SubStationId = 83, SubStation = site };
        var item = new ServiceItem { Id = 831, Code = "Item831", NameAr = "بند", NameEn = "Item", IsActive = true };
        _db.AddRange(site, gen, item);
        await _db.SaveChangesAsync();

        var clientReqId = Guid.NewGuid();
        // Validation failure: HoursAtService is negative
        var failResult = await _replacementService.RecordReplacementAsync(new RecordReplacementRequestDto(
            GeneratorId: 830,
            ServiceItemId: 831,
            HoursAtService: -10m,
            OccurredAtUtc: DateTime.UtcNow,
            ClientRequestId: clientReqId), 1);

        Assert.False(failResult.IsSuccess);

        // Verify that the idempotency store has no entry for a request rejected
        // before the command reaches its persistence boundary.
        var keyExists = await _db.IdempotencyRequests.AnyAsync(k => k.ClientRequestId == clientReqId);
        Assert.False(keyExists);

        // Now user fixes hours and resubmits with same clientRequestId
        var successResult = await _replacementService.RecordReplacementAsync(new RecordReplacementRequestDto(
            GeneratorId: 830,
            ServiceItemId: 831,
            HoursAtService: 500m,
            OccurredAtUtc: DateTime.UtcNow,
            ClientRequestId: clientReqId), 1);

        Assert.True(successResult.IsSuccess);
    }

    private sealed class DeniedScope : IUserScopeFilterService
    {
        public Task<List<int>> GetAccessibleSubStationIdsAsync(int userId) => Task.FromResult(new List<int>());
        public Task<List<int>> GetAccessibleStationIdsAsync(int userId) => Task.FromResult(new List<int>());
        public Task<List<int>> GetAccessibleBranchIdsAsync(int userId) => Task.FromResult(new List<int>());
        public Task<List<int>> GetAccessibleMeterIdsAsync(int userId) => Task.FromResult(new List<int>());
        public Task<List<int>> GetUserIdsWithAccessToSubStationAsync(int subStationId) => Task.FromResult(new List<int>());
        public Task<bool> HasAccessToSubStationAsync(int userId, int subStationId) => Task.FromResult(false);
        public Task<bool> HasFullAccessToStationAsync(int userId, int stationId) => Task.FromResult(false);
    }

    public void Dispose() => _db.Dispose();
}
