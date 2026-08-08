using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIUsageMonitor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Providers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Providers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "ProviderConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConnectionType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AccountDisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastSuccessfulSync = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastAttempt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastErrorCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    LastErrorMessage = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    CredentialReference = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProviderConnections_Providers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuotaDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuotaDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuotaDefinitions_Providers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    OriginalStartDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    BillingPeriodStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    BillingPeriodEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RenewalDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CancelledDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AutoRenew = table.Column<bool>(type: "bit", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    Cadence = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Confidence = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    LastVerifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Providers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SyncEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    DataChanged = table.Column<bool>(type: "bit", nullable: false),
                    ErrorCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ErrorSummary = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyncEvents_Providers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AlertRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuotaDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WarningThreshold = table.Column<double>(type: "float", nullable: false),
                    CriticalThreshold = table.Column<double>(type: "float", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertRules_Providers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AlertRules_QuotaDefinitions_QuotaDefinitionId",
                        column: x => x.QuotaDefinitionId,
                        principalTable: "QuotaDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UsageSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuotaDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    QuotaType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    QuotaUnit = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    UsedValue = table.Column<double>(type: "float", nullable: true),
                    RemainingValue = table.Column<double>(type: "float", nullable: true),
                    LimitValue = table.Column<double>(type: "float", nullable: true),
                    UsedPercentage = table.Column<double>(type: "float", nullable: true),
                    RemainingPercentage = table.Column<double>(type: "float", nullable: true),
                    WindowStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResetAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Confidence = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsageSnapshots_Providers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UsageSnapshots_QuotaDefinitions_QuotaDefinitionId",
                        column: x => x.QuotaDefinitionId,
                        principalTable: "QuotaDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AlertEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlertRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TriggeredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Value = table.Column<double>(type: "float", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertEvents_AlertRules_AlertRuleId",
                        column: x => x.AlertRuleId,
                        principalTable: "AlertRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertEvents_AlertRuleId_TriggeredAt",
                table: "AlertEvents",
                columns: new[] { "AlertRuleId", "TriggeredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AlertRules_ProviderId",
                table: "AlertRules",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertRules_QuotaDefinitionId",
                table: "AlertRules",
                column: "QuotaDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderConnections_ProviderId",
                table: "ProviderConnections",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Providers_Code",
                table: "Providers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuotaDefinitions_ProviderId_ExternalKey",
                table: "QuotaDefinitions",
                columns: new[] { "ProviderId", "ExternalKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_ProviderId",
                table: "Subscriptions",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_SyncEvents_ProviderId_StartedAt",
                table: "SyncEvents",
                columns: new[] { "ProviderId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UsageSnapshots_ProviderId_QuotaDefinitionId",
                table: "UsageSnapshots",
                columns: new[] { "ProviderId", "QuotaDefinitionId" });

            migrationBuilder.CreateIndex(
                name: "IX_UsageSnapshots_QuotaDefinitionId",
                table: "UsageSnapshots",
                column: "QuotaDefinitionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertEvents");

            migrationBuilder.DropTable(
                name: "ProviderConnections");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropTable(
                name: "SyncEvents");

            migrationBuilder.DropTable(
                name: "UsageSnapshots");

            migrationBuilder.DropTable(
                name: "AlertRules");

            migrationBuilder.DropTable(
                name: "QuotaDefinitions");

            migrationBuilder.DropTable(
                name: "Providers");
        }
    }
}
