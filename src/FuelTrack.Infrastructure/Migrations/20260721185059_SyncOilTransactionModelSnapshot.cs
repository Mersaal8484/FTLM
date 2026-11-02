using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FuelTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncOilTransactionModelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_OilTransactions_Oils_OilId')
    ALTER TABLE OilTransactions DROP CONSTRAINT FK_OilTransactions_Oils_OilId;
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_OilTransactions_SubStations_SourceSubStationId')
    ALTER TABLE OilTransactions DROP CONSTRAINT FK_OilTransactions_SubStations_SourceSubStationId;
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_OilTransactions_SubStations_DestinationSubStationId')
    ALTER TABLE OilTransactions DROP CONSTRAINT FK_OilTransactions_SubStations_DestinationSubStationId;
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OilTransactions_DestinationSubStationId' AND object_id = OBJECT_ID('OilTransactions'))
    DROP INDEX IX_OilTransactions_DestinationSubStationId ON OilTransactions;
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OilTransactions_SourceSubStationId' AND object_id = OBJECT_ID('OilTransactions'))
    DROP INDEX IX_OilTransactions_SourceSubStationId ON OilTransactions;
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OilTransactions_OilId' AND object_id = OBJECT_ID('OilTransactions'))
    DROP INDEX IX_OilTransactions_OilId ON OilTransactions;");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OilTransactions') AND name = 'DestinationSubStationId')
    ALTER TABLE OilTransactions DROP COLUMN DestinationSubStationId;
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OilTransactions') AND name = 'DriverIdCard')
    ALTER TABLE OilTransactions DROP COLUMN DriverIdCard;
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OilTransactions') AND name = 'DriverName')
    ALTER TABLE OilTransactions DROP COLUMN DriverName;
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OilTransactions') AND name = 'InvoiceDate')
    ALTER TABLE OilTransactions DROP COLUMN InvoiceDate;
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OilTransactions') AND name = 'InvoiceQuantity')
    ALTER TABLE OilTransactions DROP COLUMN InvoiceQuantity;
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OilTransactions') AND name = 'OilId')
    ALTER TABLE OilTransactions DROP COLUMN OilId;
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OilTransactions') AND name = 'SourceSubStationId')
    ALTER TABLE OilTransactions DROP COLUMN SourceSubStationId;
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OilTransactions') AND name = 'TruckNumber')
    ALTER TABLE OilTransactions DROP COLUMN TruckNumber;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DestinationSubStationId",
                table: "OilTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverIdCard",
                table: "OilTransactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DriverName",
                table: "OilTransactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "InvoiceDate",
                table: "OilTransactions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "InvoiceQuantity",
                table: "OilTransactions",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OilId",
                table: "OilTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceSubStationId",
                table: "OilTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TruckNumber",
                table: "OilTransactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OilTransactions_DestinationSubStationId",
                table: "OilTransactions",
                column: "DestinationSubStationId");

            migrationBuilder.CreateIndex(
                name: "IX_OilTransactions_OilId",
                table: "OilTransactions",
                column: "OilId");

            migrationBuilder.CreateIndex(
                name: "IX_OilTransactions_SourceSubStationId",
                table: "OilTransactions",
                column: "SourceSubStationId");

            migrationBuilder.AddForeignKey(
                name: "FK_OilTransactions_Oils_OilId",
                table: "OilTransactions",
                column: "OilId",
                principalTable: "Oils",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_OilTransactions_SubStations_DestinationSubStationId",
                table: "OilTransactions",
                column: "DestinationSubStationId",
                principalTable: "SubStations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OilTransactions_SubStations_SourceSubStationId",
                table: "OilTransactions",
                column: "SourceSubStationId",
                principalTable: "SubStations",
                principalColumn: "Id");
        }
    }
}
