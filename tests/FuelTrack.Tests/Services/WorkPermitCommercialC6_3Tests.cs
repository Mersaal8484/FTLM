using System.Security.Claims;
using FuelTrack.Application.Common;
using FuelTrack.Application.DTOs.Maintenance;
using FuelTrack.Domain.Enums;
using FuelTrack.Domain.Models;
using FuelTrack.Infrastructure;
using FuelTrack.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FuelTrack.Tests.Services;

public sealed class WorkPermitCommercialC6_3Tests : IDisposable
{
    private readonly FuelTrackDbContext _db;

    public WorkPermitCommercialC6_3Tests()
    {
        _db = new FuelTrackDbContext(new DbContextOptionsBuilder<FuelTrackDbContext>()
            .UseInMemoryDatabase($"WorkPermitC6_3_{Guid.NewGuid()}").Options);

        var maintTypeCat = new LookupCategory { Id = 1, Name = "MaintenanceType", DisplayName = "Maintenance Type", IsActive = true };
        var deptCat = new LookupCategory { Id = 2, Name = "MaintenanceDepartment", DisplayName = "Department", IsActive = true };
        var priorityCat = new LookupCategory { Id = 3, Name = "MaintenancePriority", DisplayName = "Priority", IsActive = true };
        var statusCat = new LookupCategory { Id = 4, Name = "MaintenanceStatus", DisplayName = "Status", IsActive = true };

        _db.AddRange(
            maintTypeCat, deptCat, priorityCat, statusCat,
            new Lookup { Id = 10, CategoryId = 1, Category = maintTypeCat, Code = "Corrective", Name = "تصحيحية", IsActive = true },
            new Lookup { Id = 20, CategoryId = 2, Category = deptCat, Code = "Electrical", Name = "كهرباء", IsActive = true },
            new Lookup { Id = 30, CategoryId = 3, Category = priorityCat, Code = "Normal", Name = "عادية", IsActive = true },
            new Lookup { Id = 40, CategoryId = 4, Category = statusCat, Code = "Draft", Name = "مسودة", IsActive = true },
            new Lookup { Id = 41, CategoryId = 4, Category = statusCat, Code = "Submitted", Name = "مقدم", IsActive = true },
            new Lookup { Id = 42, CategoryId = 4, Category = statusCat, Code = "Approved", Name = "معتمد", IsActive = true },
            new Lookup { Id = 43, CategoryId = 4, Category = statusCat, Code = "Active", Name = "نشط", IsActive = true },
            new Lookup { Id = 44, CategoryId = 4, Category = statusCat, Code = "Completed", Name = "مكتمل", IsActive = true },
            new Lookup { Id = 45, CategoryId = 4, Category = statusCat, Code = "Rejected", Name = "مرفوض", IsActive = true },
            new Lookup { Id = 46, CategoryId = 4, Category = statusCat, Code = "Cancelled", Name = "ملغى", IsActive = true },

            new Branch { Id = 1, Name = "Technical Branch", Code = "TECH-PS01", IsActive = true },
            new Station { Id = 1, BranchId = 1, Name = "Commercial Station Central", Code = "PS01", IsActive = true },
            new SubStation { Id = 1, StationId = 1, Name = "Commercial Station Central", Code = "PS01", IsActive = true },

            new Branch { Id = 2, Name = "Enterprise Branch", Code = "ENT-B1", IsActive = true },
            new Station { Id = 2, BranchId = 2, Name = "Enterprise Station", Code = "ENT01", IsActive = true },
            new SubStation { Id = 2, StationId = 2, Name = "Enterprise SubStation 2", Code = "SUB2", IsActive = false },

            new Generator { Id = 101, SubStationId = 1, Name = "Gen 1", SerialNumber = "GEN01-SN", IsActive = true },
            new Generator { Id = 102, SubStationId = 2, Name = "Gen 2", SerialNumber = "GEN02-SN", IsActive = true },

            new User { Id = 1, UserName = "admin@ft.com", Email = "admin@ft.com", FullName = "Admin User", IsActive = true },
            new User { Id = 2, UserName = "operator@ft.com", Email = "operator@ft.com", FullName = "Operator Ali", IsActive = true },
            new User { Id = 3, UserName = "maint@ft.com", Email = "maint@ft.com", FullName = "Maintenance Tech Omar", IsActive = true },
            new User { Id = 4, UserName = "manager@ft.com", Email = "manager@ft.com", FullName = "Operations Manager Tariq", IsActive = true }
        );

        // Roles
        var roleAdmin = new Role { Id = 1, Name = "Admin", NormalizedName = "ADMIN" };
        var roleOperator = new Role { Id = 2, Name = "Operator", NormalizedName = "OPERATOR" };
        var roleMaintenance = new Role { Id = 3, Name = "Maintenance", NormalizedName = "MAINTENANCE" };
        var roleManager = new Role { Id = 4, Name = "Manager", NormalizedName = "MANAGER" };
        _db.Roles.AddRange(roleAdmin, roleOperator, roleMaintenance, roleManager);

        _db.UserRoles.AddRange(
            new IdentityUserRole<int> { UserId = 1, RoleId = 1 },
            new IdentityUserRole<int> { UserId = 2, RoleId = 2 },
            new IdentityUserRole<int> { UserId = 3, RoleId = 3 },
            new IdentityUserRole<int> { UserId = 4, RoleId = 4 }
        );

        // Modules & Permissions
        var maintModule = new ModuleDefinition { Id = 1, Key = "maintenance", NameAr = "الصيانة", NameEn = "Maintenance", IsActive = true };
        _db.ModuleDefinitions.Add(maintModule);

        var pCreate = new PagePermission { Id = 1, ModuleDefinitionId = 1, ModuleDefinition = maintModule, ActionKey = "create", NameAr = "إنشاء", NameEn = "Create", IsActive = true };
        var pUpdate = new PagePermission { Id = 2, ModuleDefinitionId = 1, ModuleDefinition = maintModule, ActionKey = "update", NameAr = "تعديل", NameEn = "Update", IsActive = true };
        var pApprove = new PagePermission { Id = 3, ModuleDefinitionId = 1, ModuleDefinition = maintModule, ActionKey = "approve", NameAr = "اعتماد", NameEn = "Approve", IsActive = true };
        _db.PagePermissions.AddRange(pCreate, pUpdate, pApprove);

        _db.RolePermissions.AddRange(
            // Admin has all
            new RolePermission { RoleId = 1, PagePermissionId = 1, IsGranted = true },
            new RolePermission { RoleId = 1, PagePermissionId = 2, IsGranted = true },
            new RolePermission { RoleId = 1, PagePermissionId = 3, IsGranted = true },
            // Operator has create
            new RolePermission { RoleId = 2, PagePermissionId = 1, IsGranted = true },
            // Maintenance has update
            new RolePermission { RoleId = 3, PagePermissionId = 2, IsGranted = true },
            // Manager has update and approve
            new RolePermission { RoleId = 4, PagePermissionId = 2, IsGranted = true },
            new RolePermission { RoleId = 4, PagePermissionId = 3, IsGranted = true }
        );

        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private void EnableCommercialMode()
    {
        var existing = _db.CustomerFeatureSettings.FirstOrDefault(f => f.Key == CommercialStationModeService.FeatureKey);
        if (existing != null)
        {
            existing.IsEnabled = true;
            existing.Value = CommercialStationModeService.CommercialModeValue;
        }
        else
        {
            _db.CustomerFeatureSettings.Add(new CustomerFeatureSetting
            {
                Key = CommercialStationModeService.FeatureKey,
                IsEnabled = true,
                Value = CommercialStationModeService.CommercialModeValue,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        _db.SaveChanges();
    }

    // ==========================================
    // 1) NUMBERING TESTS
    // ==========================================

    [Fact]
    public async Task Numbering_CommercialMode_FormatMatchesOwnerConfirmed()
    {
        EnableCommercialMode();
        var service = new WorkPermitService(_db, new StubCurrentUserService { UserId = 1, IsAdmin = true }, new StubUserScopeFilterService(), new CommercialStationModeService(_db));

        var result = await service.CreateAsync(new CreateWorkPermitDto(
            SubStationId: 0,
            GeneratorId: 101,
            MaintenanceTypeLookupId: 10,
            DepartmentLookupId: 20,
            PriorityLookupId: 30,
            PermitDate: DateTime.UtcNow,
            ProblemDescription: "Test permit"), 1);

        Assert.True(result.IsSuccess);
        Assert.Equal("PS01-WP-00001", result.Value!.PermitNumber);
    }

    [Fact]
    public async Task Numbering_CommercialMode_SequenceIncrementsProperly()
    {
        EnableCommercialMode();
        var service = new WorkPermitService(_db, new StubCurrentUserService { UserId = 1, IsAdmin = true }, new StubUserScopeFilterService(), new CommercialStationModeService(_db));

        var p1 = await service.CreateAsync(new CreateWorkPermitDto(0, 101, 10, 20, 30, DateTime.UtcNow, "P1"), 1);
        var p2 = await service.CreateAsync(new CreateWorkPermitDto(0, 101, 10, 20, 30, DateTime.UtcNow, "P2"), 1);

        Assert.True(p1.IsSuccess);
        Assert.True(p2.IsSuccess);
        Assert.Equal("PS01-WP-00001", p1.Value!.PermitNumber);
        Assert.Equal("PS01-WP-00002", p2.Value!.PermitNumber);
    }

    [Fact]
    public async Task Numbering_CommercialMode_ConcurrentCreations_ProduceDistinctConsecutiveNumbers()
    {
        EnableCommercialMode();
        var service = new WorkPermitService(_db, new StubCurrentUserService { UserId = 1, IsAdmin = true }, new StubUserScopeFilterService(), new CommercialStationModeService(_db));

        var task1 = service.CreateAsync(new CreateWorkPermitDto(0, 101, 10, 20, 30, DateTime.UtcNow, "Concurrent 1"), 1);
        var task2 = service.CreateAsync(new CreateWorkPermitDto(0, 101, 10, 20, 30, DateTime.UtcNow, "Concurrent 2"), 1);

        var results = await Task.WhenAll(task1, task2);

        Assert.True(results[0].IsSuccess);
        Assert.True(results[1].IsSuccess);

        var numbers = new HashSet<string> { results[0].Value!.PermitNumber, results[1].Value!.PermitNumber };
        Assert.Equal(2, numbers.Count);
        Assert.Contains("PS01-WP-00001", numbers);
        Assert.Contains("PS01-WP-00002", numbers);
    }

    [Fact]
    public async Task Numbering_CommercialMode_MissingOrBlankStationCode_FailsCreationWithClearError()
    {
        EnableCommercialMode();
        var station = await _db.Stations.FindAsync(1);
        station!.Code = "   ";
        await _db.SaveChangesAsync();

        var service = new WorkPermitService(_db, new StubCurrentUserService { UserId = 1, IsAdmin = true }, new StubUserScopeFilterService(), new CommercialStationModeService(_db));

        var result = await service.CreateAsync(new CreateWorkPermitDto(0, 101, 10, 20, 30, DateTime.UtcNow, "P1"), 1);

        Assert.False(result.IsSuccess);
        Assert.Contains("Commercial station code is missing or blank", result.Error);
    }

    [Fact]
    public async Task Numbering_EnterpriseMode_FormatUnchanged()
    {
        // Enterprise mode: commercial not enabled
        var sub2 = await _db.SubStations.FindAsync(2);
        sub2!.IsActive = true;
        await _db.SaveChangesAsync();

        var service = new WorkPermitService(_db, new StubCurrentUserService { UserId = 1, IsAdmin = true }, new StubUserScopeFilterService(), new CommercialStationModeService(_db));

        var result = await service.CreateAsync(new CreateWorkPermitDto(
            SubStationId: 2,
            GeneratorId: 102,
            MaintenanceTypeLookupId: 10,
            DepartmentLookupId: 20,
            PriorityLookupId: 30,
            PermitDate: DateTime.UtcNow,
            ProblemDescription: "Enterprise permit"), 1);

        Assert.True(result.IsSuccess);
        var year = DateTime.UtcNow.Year;
        Assert.Equal($"WP-{year}-SUB2-0001", result.Value!.PermitNumber);
    }

    // ==========================================
    // 2) WORKFLOW & PERMISSION TESTS
    // ==========================================

    [Fact]
    public async Task Workflow_OperatorCanSubmit_DraftToSubmitted_WritesLog()
    {
        EnableCommercialMode();
        var service = new WorkPermitService(_db, new StubCurrentUserService { UserId = 2, IsAdmin = false }, new StubUserScopeFilterService(), new CommercialStationModeService(_db));

        // Create permit as operator
        var createResult = await service.CreateAsync(new CreateWorkPermitDto(0, 101, 10, 20, 30, DateTime.UtcNow, "Fix oil leak"), 2);
        Assert.True(createResult.IsSuccess);
        var permitId = createResult.Value!.Id;

        // Operator submits: Draft -> Submitted (status lookup 41)
        var submitResult = await service.UpdateStatusAsync(permitId, new UpdateWorkPermitStatusDto(41, "Submitted for review", null, null, null), 2);

        Assert.True(submitResult.IsSuccess);
        Assert.Equal(41, submitResult.Value!.StatusLookupId);
        Assert.Equal("مقدم", submitResult.Value!.StatusName);

        // Verify log was written
        var log = await _db.MaintenanceLogs.FirstOrDefaultAsync(l => l.WorkPermitId == permitId && l.Action.Contains("Draft->Submitted"));
        Assert.NotNull(log);
        Assert.Equal(2, log.LoggedById);
    }

    [Fact]
    public async Task Workflow_OperatorCannotApprove_ReturnsForbidden()
    {
        EnableCommercialMode();
        var service = new WorkPermitService(_db, new StubCurrentUserService { UserId = 2, IsAdmin = false }, new StubUserScopeFilterService(), new CommercialStationModeService(_db));

        var createResult = await service.CreateAsync(new CreateWorkPermitDto(0, 101, 10, 20, 30, DateTime.UtcNow, "Fix pump"), 2);
        var permitId = createResult.Value!.Id;

        // Move to Submitted first
        await service.UpdateStatusAsync(permitId, new UpdateWorkPermitStatusDto(41, "Submitted", null, null, null), 2);

        // Operator tries to approve: Submitted -> Approved (lookup 42)
        var approveResult = await service.UpdateStatusAsync(permitId, new UpdateWorkPermitStatusDto(42, "Trying to approve myself", null, null, null), 2);

        Assert.False(approveResult.IsSuccess);
        Assert.Equal(403, approveResult.StatusCode);
        Assert.Contains("permission", approveResult.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Workflow_MaintenanceCannotApprove_ReturnsForbidden()
    {
        EnableCommercialMode();
        var service = new WorkPermitService(_db, new StubCurrentUserService { UserId = 3, IsAdmin = false }, new StubUserScopeFilterService(), new CommercialStationModeService(_db));

        // Seed submitted permit
        var permit = new WorkPermit
        {
            PermitNumber = "PS01-WP-00005",
            SubStationId = 1,
            GeneratorId = 101,
            MaintenanceTypeLookupId = 10,
            DepartmentLookupId = 20,
            PriorityLookupId = 30,
            StatusLookupId = 41, // Submitted
            PermitDate = DateTime.UtcNow,
            ProblemDescription = "Valve stuck",
            RequestedById = 2,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.WorkPermits.Add(permit);
        await _db.SaveChangesAsync();

        // Maintenance technician (UserId 3) tries to approve
        var approveResult = await service.ApproveAsync(permit.Id, "Maintenance approving", 3);

        Assert.False(approveResult.IsSuccess);
        Assert.Equal(403, approveResult.StatusCode);
    }

    [Fact]
    public async Task Workflow_ManagerCanApprove_SetsApprovedByAndWritesLog()
    {
        EnableCommercialMode();
        var service = new WorkPermitService(_db, new StubCurrentUserService { UserId = 4, IsAdmin = false }, new StubUserScopeFilterService(), new CommercialStationModeService(_db));

        var permit = new WorkPermit
        {
            PermitNumber = "PS01-WP-00006",
            SubStationId = 1,
            GeneratorId = 101,
            MaintenanceTypeLookupId = 10,
            DepartmentLookupId = 20,
            PriorityLookupId = 30,
            StatusLookupId = 41, // Submitted
            PermitDate = DateTime.UtcNow,
            ProblemDescription = "Filter replacement",
            RequestedById = 2,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.WorkPermits.Add(permit);
        await _db.SaveChangesAsync();

        // Operations Manager approves
        var result = await service.ApproveAsync(permit.Id, "Approved by Operations Manager", 4);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value!.StatusLookupId);
        Assert.Equal("معتمد", result.Value!.StatusName);
        Assert.Equal(4, result.Value.ApprovedById);

        // Verify maintenance log
        var log = await _db.MaintenanceLogs.FirstOrDefaultAsync(l => l.WorkPermitId == permit.Id && l.Action.Contains("Submitted->Approved"));
        Assert.NotNull(log);
        Assert.Equal(4, log.LoggedById);
    }

    [Fact]
    public async Task Workflow_ManagerCanReject_SetsRejectionReason()
    {
        EnableCommercialMode();
        var service = new WorkPermitService(_db, new StubCurrentUserService { UserId = 4, IsAdmin = false }, new StubUserScopeFilterService(), new CommercialStationModeService(_db));

        var permit = new WorkPermit
        {
            PermitNumber = "PS01-WP-00007",
            SubStationId = 1,
            GeneratorId = 101,
            MaintenanceTypeLookupId = 10,
            DepartmentLookupId = 20,
            PriorityLookupId = 30,
            StatusLookupId = 41, // Submitted
            PermitDate = DateTime.UtcNow,
            ProblemDescription = "Non-essential overhaul",
            RequestedById = 2,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.WorkPermits.Add(permit);
        await _db.SaveChangesAsync();

        var result = await service.RejectAsync(permit.Id, "Scope not justified at this time", 4);

        Assert.True(result.IsSuccess);
        Assert.Equal(45, result.Value!.StatusLookupId);
        Assert.Equal("مرفوض", result.Value!.StatusName);
        Assert.Equal("Scope not justified at this time", result.Value.RejectionReason);
    }

    [Fact]
    public async Task Workflow_MaintenanceActivatesAndCompletesPermit()
    {
        EnableCommercialMode();
        var service = new WorkPermitService(_db, new StubCurrentUserService { UserId = 3, IsAdmin = false }, new StubUserScopeFilterService(), new CommercialStationModeService(_db));

        var permit = new WorkPermit
        {
            PermitNumber = "PS01-WP-00008",
            SubStationId = 1,
            GeneratorId = 101,
            MaintenanceTypeLookupId = 10,
            DepartmentLookupId = 20,
            PriorityLookupId = 30,
            StatusLookupId = 42, // Approved
            PermitDate = DateTime.UtcNow,
            ProblemDescription = "Scheduled maintenance",
            RequestedById = 2,
            ApprovedById = 4,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.WorkPermits.Add(permit);
        await _db.SaveChangesAsync();

        // Approved -> Active
        var activeResult = await service.UpdateStatusAsync(permit.Id, new UpdateWorkPermitStatusDto(43, "Starting maintenance", null, DateTime.UtcNow, null), 3);
        Assert.True(activeResult.IsSuccess);
        Assert.Equal(43, activeResult.Value!.StatusLookupId);
        Assert.Equal("نشط", activeResult.Value!.StatusName);

        // Active -> Completed
        var completedResult = await service.UpdateStatusAsync(permit.Id, new UpdateWorkPermitStatusDto(44, "Maintenance done successfully", null, null, DateTime.UtcNow), 3);
        Assert.True(completedResult.IsSuccess);
        Assert.Equal(44, completedResult.Value!.StatusLookupId);
        Assert.Equal("مكتمل", completedResult.Value!.StatusName);
    }

    [Fact]
    public async Task Workflow_CompletedStatusIsTerminal_CannotTransition()
    {
        EnableCommercialMode();
        var service = new WorkPermitService(_db, new StubCurrentUserService { UserId = 1, IsAdmin = true }, new StubUserScopeFilterService(), new CommercialStationModeService(_db));

        var permit = new WorkPermit
        {
            PermitNumber = "PS01-WP-00009",
            SubStationId = 1,
            MaintenanceTypeLookupId = 10,
            DepartmentLookupId = 20,
            PriorityLookupId = 30,
            StatusLookupId = 44, // Completed
            PermitDate = DateTime.UtcNow,
            ProblemDescription = "Done job",
            RequestedById = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.WorkPermits.Add(permit);
        await _db.SaveChangesAsync();

        // Attempting to reopen or change status of completed permit fails
        var result = await service.UpdateStatusAsync(permit.Id, new UpdateWorkPermitStatusDto(43, "Try to reactivate", null, null, null), 1);

        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.StatusCode);
        Assert.Contains("terminal", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Workflow_InvalidDirectTransition_ReturnsConflict()
    {
        EnableCommercialMode();
        var service = new WorkPermitService(_db, new StubCurrentUserService { UserId = 1, IsAdmin = true }, new StubUserScopeFilterService(), new CommercialStationModeService(_db));

        var createResult = await service.CreateAsync(new CreateWorkPermitDto(0, 101, 10, 20, 30, DateTime.UtcNow, "Draft permit"), 1);
        var permitId = createResult.Value!.Id;

        // Draft -> Completed directly is invalid
        var result = await service.UpdateStatusAsync(permitId, new UpdateWorkPermitStatusDto(44, "Skipping all steps", null, null, null), 1);

        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.StatusCode);
        Assert.Contains("invalid", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    // ==========================================
    // 3) PER-PERMIT REPORT SHEET TESTS
    // ==========================================

    [Fact]
    public async Task ReportSheet_RetrievesAllFields_WithStationTerminologyAndOilConsumed()
    {
        EnableCommercialMode();

        var permit = new WorkPermit
        {
            PermitNumber = "PS01-WP-00015",
            SubStationId = 1,
            GeneratorId = 101,
            MaintenanceTypeLookupId = 10,
            DepartmentLookupId = 20,
            PriorityLookupId = 30,
            StatusLookupId = 42, // Approved
            PermitDate = DateTime.UtcNow,
            ProblemDescription = "500h oil and filter change",
            ProblemCause = "Routine wear",
            RequestedById = 2,
            ApprovedById = 4,
            AssignedToId = 3,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.WorkPermits.Add(permit);
        await _db.SaveChangesAsync();

        // Tasks
        _db.WorkTasks.Add(new WorkTask
        {
            WorkPermitId = permit.Id,
            TaskDescription = "Drain old engine oil",
            SortOrder = 1,
            IsCompleted = true,
            CompletedAt = DateTime.UtcNow,
            CompletedById = 3
        });

        // Spare part & dispatch request
        var unitCat = new LookupCategory { Id = 5, Name = "UnitOfMeasure", DisplayName = "Unit of Measure", IsActive = true };
        var partCat = new LookupCategory { Id = 6, Name = "PartCategory", DisplayName = "Part Category", IsActive = true };
        var oilTypeCat = new LookupCategory { Id = 7, Name = "OilType", DisplayName = "Oil Type", IsActive = true };
        var oilTxTypeCat = new LookupCategory { Id = 8, Name = "OilTransactionType", DisplayName = "Oil Transaction Type", IsActive = true };

        var unitPcs = new Lookup { Id = 50, CategoryId = 5, Category = unitCat, Code = "PCS", Name = "PCS", IsActive = true };
        var partMech = new Lookup { Id = 51, CategoryId = 6, Category = partCat, Code = "Mechanical", Name = "ميكانيك", IsActive = true };
        var oil15w40 = new Lookup { Id = 60, CategoryId = 7, Category = oilTypeCat, Code = "15W40", Name = "15W40", IsActive = true };
        var txConsumption = new Lookup { Id = 70, CategoryId = 8, Category = oilTxTypeCat, Code = "Consumption", Name = "استهلاك", IsActive = true };
        _db.AddRange(unitCat, partCat, oilTypeCat, oilTxTypeCat, unitPcs, partMech, oil15w40, txConsumption);

        var part = new SparePart
        {
            Id = 1,
            Name = "Oil Filter",
            PartNumber = "OF-500",
            UnitLookupId = 50,
            UnitLookup = unitPcs,
            PartCategoryLookupId = 51,
            PartCategoryLookup = partMech,
            IsActive = true
        };
        _db.SpareParts.Add(part);

        var dispatch = new DispatchRequest
        {
            RequestNumber = "DR-001",
            StationId = 1,
            SubStationId = 1,
            WorkPermitId = permit.Id,
            StatusLookupId = 40,
            RequestedById = 2,
            RequestDate = DateTime.UtcNow
        };
        _db.DispatchRequests.Add(dispatch);

        _db.DispatchRequestItems.Add(new DispatchRequestItem
        {
            DispatchRequest = dispatch,
            DispatchRequestId = dispatch.Id,
            SparePart = part,
            SparePartId = 1,
            RequestedQuantity = 2m,
            ApprovedQuantity = 2m,
            IsApproved = true
        });

        // Service replacement with C6.2 oil transaction link
        var serviceItem = new ServiceItem
        {
            Id = 1,
            NameAr = "زيت محرك 15W40",
            NameEn = "Engine Oil 15W40",
            Code = "OIL-15W40",
            IsActive = true
        };
        _db.ServiceItems.Add(serviceItem);

        var oilTx = new OilTransaction
        {
            SubStationId = 1,
            OilLookupId = 60,
            OilLookup = oil15w40,
            TransactionTypeLookupId = 70,
            TransactionTypeLookup = txConsumption,
            TransactionDate = DateTime.UtcNow,
            QuantityLiters = 45.0,
            Notes = "500h maintenance oil fill",
            IsApproved = true
        };
        _db.OilTransactions.Add(oilTx);
        await _db.SaveChangesAsync();

        var replacement = new ServiceReplacementRecord
        {
            GeneratorId = 101,
            ServiceItemId = 1,
            Kind = ServiceRecordKind.Replacement,
            HoursAtService = 500m,
            OccurredAtUtc = DateTime.UtcNow,
            WorkPermitId = permit.Id,
            OilTransactionId = oilTx.Id
        };
        _db.ServiceReplacementRecords.Add(replacement);

        _db.MaintenanceLogs.Add(new MaintenanceLog
        {
            WorkPermitId = permit.Id,
            GeneratorId = 101,
            LogDate = DateTime.UtcNow,
            Action = "StatusChanged:Submitted->Approved",
            Notes = "Approved by Manager Tariq",
            LoggedById = 4
        });
        await _db.SaveChangesAsync();

        var service = new WorkPermitService(_db, new StubCurrentUserService { UserId = 1, IsAdmin = true }, new StubUserScopeFilterService(), new CommercialStationModeService(_db));

        var sheet = await service.GetSheetAsync(permit.Id);

        Assert.NotNull(sheet);
        Assert.Equal("PS01-WP-00015", sheet.PermitNumber);
        // Commercial terminology: station only!
        Assert.Equal("Commercial Station Central", sheet.StationName);
        Assert.Equal("Gen 1", sheet.GeneratorName);
        Assert.Equal("GEN01-SN", sheet.GeneratorSerialNumber);
        Assert.Equal("Operator Ali", sheet.RequesterName);
        Assert.Equal("Operations Manager Tariq", sheet.ApproverName);
        Assert.Equal("Maintenance Tech Omar", sheet.AssignedToName);

        // Tasks
        Assert.Single(sheet.Tasks);
        Assert.Equal("Drain old engine oil", sheet.Tasks[0].TaskDescription);
        Assert.True(sheet.Tasks[0].IsCompleted);

        // Dispatched parts
        Assert.Single(sheet.DispatchedParts);
        Assert.Equal("Oil Filter", sheet.DispatchedParts[0].PartName);
        Assert.Equal(2m, sheet.DispatchedParts[0].QuantityRequested);
        Assert.Equal("PCS", sheet.DispatchedParts[0].UnitOfMeasure);

        // Replacements & Oil
        Assert.Single(sheet.Replacements);
        Assert.Equal("زيت محرك 15W40", sheet.Replacements[0].ServiceItemName);
        Assert.Equal(45m, sheet.Replacements[0].OilConsumedLitres);
        Assert.Equal(45m, sheet.TotalOilConsumedLitres);

        // Logs
        Assert.Single(sheet.Logs);
        Assert.Equal("Operations Manager Tariq", sheet.Logs[0].LoggedByName);
    }
}
