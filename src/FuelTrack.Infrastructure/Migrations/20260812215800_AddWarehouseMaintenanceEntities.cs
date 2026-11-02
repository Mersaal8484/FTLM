using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FuelTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehouseMaintenanceEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SpareParts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PartNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ManufacturerPartNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Barcode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PartCategoryLookupId = table.Column<int>(type: "int", nullable: false),
                    UnitLookupId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    MinStockQuantity = table.Column<int>(type: "int", nullable: false),
                    ReplacementIntervalHours = table.Column<int>(type: "int", nullable: true),
                    ReplacementPriorityLookupId = table.Column<int>(type: "int", nullable: true),
                    IsHoursTracked = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpareParts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpareParts_Lookups_PartCategoryLookupId",
                        column: x => x.PartCategoryLookupId,
                        principalTable: "Lookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SpareParts_Lookups_ReplacementPriorityLookupId",
                        column: x => x.ReplacementPriorityLookupId,
                        principalTable: "Lookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SpareParts_Lookups_UnitLookupId",
                        column: x => x.UnitLookupId,
                        principalTable: "Lookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseSupplies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplyNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StationId = table.Column<int>(type: "int", nullable: false),
                    SupplyDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SupplierName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    StatusLookupId = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseSupplies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseSupplies_Lookups_StatusLookupId",
                        column: x => x.StatusLookupId,
                        principalTable: "Lookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseSupplies_Stations_StationId",
                        column: x => x.StationId,
                        principalTable: "Stations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkPermits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PermitNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubStationId = table.Column<int>(type: "int", nullable: false),
                    GeneratorId = table.Column<int>(type: "int", nullable: true),
                    MaintenanceTypeLookupId = table.Column<int>(type: "int", nullable: false),
                    DepartmentLookupId = table.Column<int>(type: "int", nullable: false),
                    PriorityLookupId = table.Column<int>(type: "int", nullable: false),
                    StatusLookupId = table.Column<int>(type: "int", nullable: false),
                    PermitDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProblemDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProblemCause = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlannedStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PlannedEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RunningHoursAtCreation = table.Column<double>(type: "float", nullable: true),
                    RequestedById = table.Column<int>(type: "int", nullable: false),
                    AssignedToId = table.Column<int>(type: "int", nullable: true),
                    ApprovedById = table.Column<int>(type: "int", nullable: true),
                    CompletionNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkPermits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkPermits_Generators_GeneratorId",
                        column: x => x.GeneratorId,
                        principalTable: "Generators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WorkPermits_Lookups_DepartmentLookupId",
                        column: x => x.DepartmentLookupId,
                        principalTable: "Lookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkPermits_Lookups_MaintenanceTypeLookupId",
                        column: x => x.MaintenanceTypeLookupId,
                        principalTable: "Lookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkPermits_Lookups_PriorityLookupId",
                        column: x => x.PriorityLookupId,
                        principalTable: "Lookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkPermits_Lookups_StatusLookupId",
                        column: x => x.StatusLookupId,
                        principalTable: "Lookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkPermits_SubStations_SubStationId",
                        column: x => x.SubStationId,
                        principalTable: "SubStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkPermits_Users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkPermits_Users_AssignedToId",
                        column: x => x.AssignedToId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkPermits_Users_RequestedById",
                        column: x => x.RequestedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseStocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SparePartId = table.Column<int>(type: "int", nullable: false),
                    StationId = table.Column<int>(type: "int", nullable: false),
                    CurrentQuantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    ReservedQuantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseStocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseStocks_SpareParts_SparePartId",
                        column: x => x.SparePartId,
                        principalTable: "SpareParts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseStocks_Stations_StationId",
                        column: x => x.StationId,
                        principalTable: "Stations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SparePartId = table.Column<int>(type: "int", nullable: false),
                    StationId = table.Column<int>(type: "int", nullable: false),
                    TransactionTypeLookupId = table.Column<int>(type: "int", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    BalanceBefore = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    ReferenceId = table.Column<int>(type: "int", nullable: true),
                    ReferenceType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseTransactions_Lookups_TransactionTypeLookupId",
                        column: x => x.TransactionTypeLookupId,
                        principalTable: "Lookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseTransactions_SpareParts_SparePartId",
                        column: x => x.SparePartId,
                        principalTable: "SpareParts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseTransactions_Stations_StationId",
                        column: x => x.StationId,
                        principalTable: "Stations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseSupplyItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseSupplyId = table.Column<int>(type: "int", nullable: false),
                    SparePartId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseSupplyItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseSupplyItems_SpareParts_SparePartId",
                        column: x => x.SparePartId,
                        principalTable: "SpareParts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseSupplyItems_WarehouseSupplies_WarehouseSupplyId",
                        column: x => x.WarehouseSupplyId,
                        principalTable: "WarehouseSupplies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DispatchRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubStationId = table.Column<int>(type: "int", nullable: false),
                    StationId = table.Column<int>(type: "int", nullable: false),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestedById = table.Column<int>(type: "int", nullable: false),
                    ApprovedById = table.Column<int>(type: "int", nullable: true),
                    ApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WorkPermitId = table.Column<int>(type: "int", nullable: true),
                    StatusLookupId = table.Column<int>(type: "int", nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispatchRequests_Lookups_StatusLookupId",
                        column: x => x.StatusLookupId,
                        principalTable: "Lookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchRequests_Stations_StationId",
                        column: x => x.StationId,
                        principalTable: "Stations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchRequests_SubStations_SubStationId",
                        column: x => x.SubStationId,
                        principalTable: "SubStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchRequests_Users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DispatchRequests_Users_RequestedById",
                        column: x => x.RequestedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchRequests_WorkPermits_WorkPermitId",
                        column: x => x.WorkPermitId,
                        principalTable: "WorkPermits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkPermitId = table.Column<int>(type: "int", nullable: false),
                    GeneratorId = table.Column<int>(type: "int", nullable: true),
                    LogDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RunningHoursAtLog = table.Column<double>(type: "float", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LoggedById = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceLogs_Generators_GeneratorId",
                        column: x => x.GeneratorId,
                        principalTable: "Generators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MaintenanceLogs_Users_LoggedById",
                        column: x => x.LoggedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaintenanceLogs_WorkPermits_WorkPermitId",
                        column: x => x.WorkPermitId,
                        principalTable: "WorkPermits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkPermitId = table.Column<int>(type: "int", nullable: false),
                    TaskDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedById = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkTasks_Users_CompletedById",
                        column: x => x.CompletedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WorkTasks_WorkPermits_WorkPermitId",
                        column: x => x.WorkPermitId,
                        principalTable: "WorkPermits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DispatchRequestItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DispatchRequestId = table.Column<int>(type: "int", nullable: false),
                    SparePartId = table.Column<int>(type: "int", nullable: false),
                    RequestedQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ApprovedQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IssuedQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchRequestItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispatchRequestItems_DispatchRequests_DispatchRequestId",
                        column: x => x.DispatchRequestId,
                        principalTable: "DispatchRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DispatchRequestItems_SpareParts_SparePartId",
                        column: x => x.SparePartId,
                        principalTable: "SpareParts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InstalledParts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GeneratorId = table.Column<int>(type: "int", nullable: false),
                    SparePartId = table.Column<int>(type: "int", nullable: false),
                    WorkPermitId = table.Column<int>(type: "int", nullable: true),
                    DispatchRequestItemId = table.Column<int>(type: "int", nullable: true),
                    InstallationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InstallationHours = table.Column<double>(type: "float", nullable: false),
                    ReplacementIntervalHours = table.Column<int>(type: "int", nullable: true),
                    EstimatedReplacementAtHours = table.Column<double>(type: "float", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RemovedAtHours = table.Column<double>(type: "float", nullable: true),
                    EarlyReplacementReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<int>(type: "int", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstalledParts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstalledParts_DispatchRequestItems_DispatchRequestItemId",
                        column: x => x.DispatchRequestItemId,
                        principalTable: "DispatchRequestItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InstalledParts_Generators_GeneratorId",
                        column: x => x.GeneratorId,
                        principalTable: "Generators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InstalledParts_SpareParts_SparePartId",
                        column: x => x.SparePartId,
                        principalTable: "SpareParts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InstalledParts_WorkPermits_WorkPermitId",
                        column: x => x.WorkPermitId,
                        principalTable: "WorkPermits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "LookupCategories",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "Description", "DisplayName", "IsActive", "Name", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 20, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, "Spare part categories: Mechanical, Electrical, Electronic, Instrumentation, Workshop, Other", "فئة القطعة", true, "PartCategory", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 21, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, "Spare part units: Piece, Liter, Kilogram, Meter, Set, Pack", "وحدة القطعة", true, "PartUnit", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 22, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, "Maintenance types: Routine, Preventive, Scheduled, HalfLife, FullLife, Inspection, Emergency", "نوع الصيانة", true, "MaintenanceType", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 23, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, "Maintenance departments: All, Mechanical, Electrical, Instrumentation, Workshop", "القسم المكلف", true, "MaintenanceDepartment", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 24, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, "Maintenance priority: Critical, Emergency, Normal, Low", "أولوية الصيانة", true, "MaintenancePriority", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 25, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, "Work permit status: Draft, Submitted, Active, Completed, Approved, Rejected, Cancelled", "حالة التصريح", true, "MaintenanceStatus", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 26, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, "Dispatch request status: Pending, Approved, PartialApproved, Rejected, Issued, Completed", "حالة طلب الصرف", true, "DispatchStatus", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 27, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, "Warehouse supply voucher status: Draft, Completed", "حالة سند التوريد", true, "WarehouseSupplyStatus", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 28, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, "Warehouse transaction types: Supply, Dispatch, Return, Adjustment, Transfer", "نوع حركة المخزون", true, "WarehouseTransactionType", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 29, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, "Replacement priority: Critical, High, Medium, Low", "أهمية الاستبدال", true, "ReplacementPriority", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 }
                });

            migrationBuilder.InsertData(
                table: "Lookups",
                columns: new[] { "Id", "CategoryId", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "IsActive", "Name", "SortOrder", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 60, 20, "Mechanical", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "ميكانيكي", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 61, 20, "Electrical", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "كهربائي", 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 62, 20, "Electronic", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "إلكتروني", 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 63, 20, "Instrumentation", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "أجهزة قياس", 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 64, 20, "Workshop", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "ورشة", 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 65, 20, "Other", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "أخرى", 6, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 66, 21, "Piece", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "قطعة", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 67, 21, "Liter", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "لتر", 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 68, 21, "Kilogram", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "كيلوغرام", 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 69, 21, "Meter", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "متر", 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 70, 21, "Set", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "طقم", 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 71, 21, "Pack", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "علبة", 6, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 72, 22, "Routine", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "روتيني", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 73, 22, "Preventive", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "وقائي", 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 74, 22, "Scheduled", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "مبرمج", 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 75, 22, "HalfLife", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "نصف عمرية", 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 76, 22, "FullLife", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "عمرية", 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 77, 22, "Inspection", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "تفقدية", 6, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 78, 22, "Emergency", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "طارئة", 7, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 79, 23, "All", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "الكل", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 80, 23, "Mechanical", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "ميكانيكي", 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 81, 23, "Electrical", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "كهربائي", 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 82, 23, "Instrumentation", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "أجهزة", 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 83, 23, "Workshop", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "ورشة", 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 84, 24, "Critical", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "حرجة", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 85, 24, "Emergency", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "اضطراري", 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 86, 24, "Normal", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "عادية", 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 87, 24, "Low", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "منخفضة", 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 88, 25, "Draft", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "مسودة", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 89, 25, "Submitted", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "مقدم", 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 90, 25, "Active", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "نشط", 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 91, 25, "Completed", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "مكتمل", 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 92, 25, "Approved", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "معتمد", 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 93, 25, "Rejected", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "مرفوض", 6, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 94, 25, "Cancelled", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "ملغى", 7, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 95, 26, "Pending", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "قيد الانتظار", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 96, 26, "Approved", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "معتمد", 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 97, 26, "PartialApproved", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "موافقة جزئية", 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 98, 26, "Rejected", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "مرفوض", 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 99, 26, "Issued", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "تم الصرف", 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 100, 26, "Completed", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "مكتمل", 6, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 101, 27, "Draft", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "مسودة", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 102, 27, "Completed", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "مكتمل", 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 103, 28, "Supply", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "توريد", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 104, 28, "Dispatch", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "صرف", 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 105, 28, "Return", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "إرجاع", 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 106, 28, "Adjustment", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "تسوية", 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 107, 28, "Transfer", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "نقل", 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 108, 29, "Critical", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "حرجة", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 109, 29, "High", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "عالية", 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 110, 29, "Medium", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "متوسطة", 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 },
                    { 111, 29, "Low", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, null, true, "منخفضة", 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchRequestItems_DispatchRequestId",
                table: "DispatchRequestItems",
                column: "DispatchRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchRequestItems_SparePartId",
                table: "DispatchRequestItems",
                column: "SparePartId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchRequest_Station_Status",
                table: "DispatchRequests",
                columns: new[] { "StationId", "StatusLookupId" });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchRequest_SubStation_Date",
                table: "DispatchRequests",
                columns: new[] { "SubStationId", "RequestDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchRequests_ApprovedById",
                table: "DispatchRequests",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchRequests_RequestedById",
                table: "DispatchRequests",
                column: "RequestedById");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchRequests_StatusLookupId",
                table: "DispatchRequests",
                column: "StatusLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchRequests_WorkPermitId",
                table: "DispatchRequests",
                column: "WorkPermitId");

            migrationBuilder.CreateIndex(
                name: "IX_InstalledPart_Generator_Active",
                table: "InstalledParts",
                columns: new[] { "GeneratorId", "IsActive" },
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InstalledParts_DispatchRequestItemId",
                table: "InstalledParts",
                column: "DispatchRequestItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InstalledParts_SparePartId",
                table: "InstalledParts",
                column: "SparePartId");

            migrationBuilder.CreateIndex(
                name: "IX_InstalledParts_WorkPermitId",
                table: "InstalledParts",
                column: "WorkPermitId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceLog_Permit_Date",
                table: "MaintenanceLogs",
                columns: new[] { "WorkPermitId", "LogDate" });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceLogs_GeneratorId",
                table: "MaintenanceLogs",
                column: "GeneratorId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceLogs_LoggedById",
                table: "MaintenanceLogs",
                column: "LoggedById");

            migrationBuilder.CreateIndex(
                name: "IX_SpareParts_PartCategoryLookupId",
                table: "SpareParts",
                column: "PartCategoryLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_SpareParts_PartNumber",
                table: "SpareParts",
                column: "PartNumber",
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SpareParts_ReplacementPriorityLookupId",
                table: "SpareParts",
                column: "ReplacementPriorityLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_SpareParts_UnitLookupId",
                table: "SpareParts",
                column: "UnitLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStocks_SparePartId_StationId",
                table: "WarehouseStocks",
                columns: new[] { "SparePartId", "StationId" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStocks_StationId",
                table: "WarehouseStocks",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseSupplies_StatusLookupId",
                table: "WarehouseSupplies",
                column: "StatusLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseSupply_Station_Date",
                table: "WarehouseSupplies",
                columns: new[] { "StationId", "SupplyDate" });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseSupplyItems_SparePartId",
                table: "WarehouseSupplyItems",
                column: "SparePartId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseSupplyItems_WarehouseSupplyId",
                table: "WarehouseSupplyItems",
                column: "WarehouseSupplyId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransactions_SparePartId",
                table: "WarehouseTransactions",
                column: "SparePartId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransactions_StationId",
                table: "WarehouseTransactions",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseTransactions_TransactionTypeLookupId",
                table: "WarehouseTransactions",
                column: "TransactionTypeLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkPermit_Status_Priority",
                table: "WorkPermits",
                columns: new[] { "StatusLookupId", "PriorityLookupId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkPermit_SubStation_Date",
                table: "WorkPermits",
                columns: new[] { "SubStationId", "PermitDate" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkPermits_ApprovedById",
                table: "WorkPermits",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_WorkPermits_AssignedToId",
                table: "WorkPermits",
                column: "AssignedToId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkPermits_DepartmentLookupId",
                table: "WorkPermits",
                column: "DepartmentLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkPermits_GeneratorId",
                table: "WorkPermits",
                column: "GeneratorId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkPermits_MaintenanceTypeLookupId",
                table: "WorkPermits",
                column: "MaintenanceTypeLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkPermits_PriorityLookupId",
                table: "WorkPermits",
                column: "PriorityLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkPermits_RequestedById",
                table: "WorkPermits",
                column: "RequestedById");

            migrationBuilder.CreateIndex(
                name: "IX_WorkTasks_CompletedById",
                table: "WorkTasks",
                column: "CompletedById");

            migrationBuilder.CreateIndex(
                name: "IX_WorkTasks_WorkPermitId",
                table: "WorkTasks",
                column: "WorkPermitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InstalledParts");

            migrationBuilder.DropTable(
                name: "MaintenanceLogs");

            migrationBuilder.DropTable(
                name: "WarehouseStocks");

            migrationBuilder.DropTable(
                name: "WarehouseSupplyItems");

            migrationBuilder.DropTable(
                name: "WarehouseTransactions");

            migrationBuilder.DropTable(
                name: "WorkTasks");

            migrationBuilder.DropTable(
                name: "DispatchRequestItems");

            migrationBuilder.DropTable(
                name: "WarehouseSupplies");

            migrationBuilder.DropTable(
                name: "DispatchRequests");

            migrationBuilder.DropTable(
                name: "SpareParts");

            migrationBuilder.DropTable(
                name: "WorkPermits");

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 60);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 61);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 62);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 63);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 64);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 65);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 66);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 67);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 68);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 69);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 70);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 71);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 72);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 73);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 74);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 75);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 76);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 77);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 78);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 79);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 80);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 81);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 82);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 83);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 84);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 85);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 86);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 87);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 88);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 89);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 90);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 91);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 92);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 93);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 94);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 95);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 96);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 97);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 98);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 99);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 100);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 101);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 102);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 103);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 104);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 105);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 106);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 107);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 108);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 109);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 110);

            migrationBuilder.DeleteData(
                table: "Lookups",
                keyColumn: "Id",
                keyValue: 111);

            migrationBuilder.DeleteData(
                table: "LookupCategories",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "LookupCategories",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "LookupCategories",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "LookupCategories",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "LookupCategories",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "LookupCategories",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "LookupCategories",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "LookupCategories",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "LookupCategories",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "LookupCategories",
                keyColumn: "Id",
                keyValue: 29);
        }
    }
}
