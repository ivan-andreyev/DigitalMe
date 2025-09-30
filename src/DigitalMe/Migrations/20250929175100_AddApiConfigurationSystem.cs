using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DigitalMe.Migrations
{
    /// <inheritdoc />
    public partial class AddApiConfigurationSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    EncryptedApiKey = table.Column<string>(type: "TEXT", nullable: false),
                    EncryptionIV = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EncryptionSalt = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    KeyFingerprint = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    LastUsedAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    LastValidatedAt = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    ValidationStatus = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, defaultValue: "Unknown"),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ErrorPatterns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PatternHash = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Subcategory = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    HttpStatusCode = table.Column<int>(type: "INTEGER", nullable: true),
                    ApiEndpoint = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    HttpMethod = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    OccurrenceCount = table.Column<int>(type: "INTEGER", nullable: false),
                    FirstObserved = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    LastObserved = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    SeverityLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    ConfidenceScore = table.Column<double>(type: "REAL", nullable: false),
                    Context = table.Column<string>(type: "jsonb", nullable: true),
                    SuggestedSolutions = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorPatterns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApiUsageRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ConfigurationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Model = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    RequestType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    TokensUsed = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    InputTokens = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    OutputTokens = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    CostEstimate = table.Column<decimal>(type: "decimal(10,6)", nullable: false, defaultValue: 0m),
                    ResponseTimeMs = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    Success = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ErrorType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    RequestTimestamp = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiUsageRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiUsageRecords_ApiConfigurations_ConfigurationId",
                        column: x => x.ConfigurationId,
                        principalTable: "ApiConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "LearningHistoryEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ErrorPatternId = table.Column<int>(type: "INTEGER", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    TestCaseName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ApiName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: false),
                    StackTrace = table.Column<string>(type: "TEXT", nullable: true),
                    RequestDetails = table.Column<string>(type: "jsonb", nullable: true),
                    ResponseDetails = table.Column<string>(type: "jsonb", nullable: true),
                    EnvironmentContext = table.Column<string>(type: "jsonb", nullable: true),
                    IsAnalyzed = table.Column<bool>(type: "INTEGER", nullable: false),
                    ContributedToPattern = table.Column<bool>(type: "INTEGER", nullable: false),
                    ConfidenceScore = table.Column<double>(type: "REAL", nullable: false),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningHistoryEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LearningHistoryEntries_ErrorPatterns_ErrorPatternId",
                        column: x => x.ErrorPatternId,
                        principalTable: "ErrorPatterns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OptimizationSuggestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ErrorPatternId = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    TargetComponent = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ExpectedImpact = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    EstimatedEffortHours = table.Column<double>(type: "REAL", nullable: true),
                    ConfidenceScore = table.Column<double>(type: "REAL", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ImplementationDetails = table.Column<string>(type: "jsonb", nullable: true),
                    Tags = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IsReviewed = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReviewerNotes = table.Column<string>(type: "TEXT", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OptimizationSuggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OptimizationSuggestions_ErrorPatterns_ErrorPatternId",
                        column: x => x.ErrorPatternId,
                        principalTable: "ErrorPatterns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiConfigurations_LastUsedAt",
                table: "ApiConfigurations",
                column: "LastUsedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ApiConfigurations_Provider",
                table: "ApiConfigurations",
                column: "Provider");

            migrationBuilder.CreateIndex(
                name: "IX_ApiConfigurations_UserId",
                table: "ApiConfigurations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiConfigurations_UserId_IsActive",
                table: "ApiConfigurations",
                columns: new[] { "UserId", "IsActive" },
                filter: "IsActive = true");

            migrationBuilder.CreateIndex(
                name: "IX_ApiConfigurations_UserId_Provider_Unique",
                table: "ApiConfigurations",
                columns: new[] { "UserId", "Provider" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiUsageRecords_ConfigurationId",
                table: "ApiUsageRecords",
                column: "ConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiUsageRecords_Provider",
                table: "ApiUsageRecords",
                column: "Provider");

            migrationBuilder.CreateIndex(
                name: "IX_ApiUsageRecords_Provider_RequestTimestamp",
                table: "ApiUsageRecords",
                columns: new[] { "Provider", "RequestTimestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_ApiUsageRecords_RequestTimestamp",
                table: "ApiUsageRecords",
                column: "RequestTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ApiUsageRecords_UserId",
                table: "ApiUsageRecords",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiUsageRecords_UserId_Provider_Success",
                table: "ApiUsageRecords",
                columns: new[] { "UserId", "Provider", "Success" });

            migrationBuilder.CreateIndex(
                name: "IX_ApiUsageRecords_UserId_RequestTimestamp",
                table: "ApiUsageRecords",
                columns: new[] { "UserId", "RequestTimestamp" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_ErrorPatterns_ApiEndpoint",
                table: "ErrorPatterns",
                column: "ApiEndpoint");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorPatterns_Category",
                table: "ErrorPatterns",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorPatterns_Category_Subcategory",
                table: "ErrorPatterns",
                columns: new[] { "Category", "Subcategory" });

            migrationBuilder.CreateIndex(
                name: "IX_ErrorPatterns_LastObserved",
                table: "ErrorPatterns",
                column: "LastObserved");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorPatterns_OccurrenceCount",
                table: "ErrorPatterns",
                column: "OccurrenceCount");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorPatterns_PatternHash",
                table: "ErrorPatterns",
                column: "PatternHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ErrorPatterns_SeverityLevel",
                table: "ErrorPatterns",
                column: "SeverityLevel");

            migrationBuilder.CreateIndex(
                name: "IX_LearningHistoryEntries_ApiName",
                table: "LearningHistoryEntries",
                column: "ApiName");

            migrationBuilder.CreateIndex(
                name: "IX_LearningHistoryEntries_ErrorPatternId",
                table: "LearningHistoryEntries",
                column: "ErrorPatternId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningHistoryEntries_IsAnalyzed_ContributedToPattern",
                table: "LearningHistoryEntries",
                columns: new[] { "IsAnalyzed", "ContributedToPattern" });

            migrationBuilder.CreateIndex(
                name: "IX_LearningHistoryEntries_Source",
                table: "LearningHistoryEntries",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_LearningHistoryEntries_Timestamp",
                table: "LearningHistoryEntries",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_OptimizationSuggestions_ErrorPatternId",
                table: "OptimizationSuggestions",
                column: "ErrorPatternId");

            migrationBuilder.CreateIndex(
                name: "IX_OptimizationSuggestions_GeneratedAt",
                table: "OptimizationSuggestions",
                column: "GeneratedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OptimizationSuggestions_IsReviewed_Status",
                table: "OptimizationSuggestions",
                columns: new[] { "IsReviewed", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OptimizationSuggestions_Priority",
                table: "OptimizationSuggestions",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_OptimizationSuggestions_Status",
                table: "OptimizationSuggestions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OptimizationSuggestions_Type",
                table: "OptimizationSuggestions",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiUsageRecords");

            migrationBuilder.DropTable(
                name: "LearningHistoryEntries");

            migrationBuilder.DropTable(
                name: "OptimizationSuggestions");

            migrationBuilder.DropTable(
                name: "ApiConfigurations");

            migrationBuilder.DropTable(
                name: "ErrorPatterns");
        }
    }
}
