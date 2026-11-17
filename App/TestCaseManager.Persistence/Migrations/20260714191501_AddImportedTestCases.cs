using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TestCaseManager.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddImportedTestCases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImportedTestCases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportedTestCases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TestCaseFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportedTestCaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Section = table.Column<int>(type: "integer", nullable: false),
                    FileNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Designation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestCaseFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestCaseFiles_ImportedTestCases_ImportedTestCaseId",
                        column: x => x.ImportedTestCaseId,
                        principalTable: "ImportedTestCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DataEntities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TestCaseFileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataEntities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataEntities_TestCaseFiles_TestCaseFileId",
                        column: x => x.TestCaseFileId,
                        principalTable: "TestCaseFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DataRows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DataEntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowIndex = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataRows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataRows_DataEntities_DataEntityId",
                        column: x => x.DataEntityId,
                        principalTable: "DataEntities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DataValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DataRowId = table.Column<Guid>(type: "uuid", nullable: false),
                    ColumnName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true),
                    IsQuoted = table.Column<bool>(type: "boolean", nullable: false),
                    DataType = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataValues_DataRows_DataRowId",
                        column: x => x.DataRowId,
                        principalTable: "DataRows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DataEntities_TestCaseFileId",
                table: "DataEntities",
                column: "TestCaseFileId");

            migrationBuilder.CreateIndex(
                name: "IX_DataRows_DataEntityId",
                table: "DataRows",
                column: "DataEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_DataValues_DataRowId",
                table: "DataValues",
                column: "DataRowId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedTestCases_Number",
                table: "ImportedTestCases",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestCaseFiles_ImportedTestCaseId",
                table: "TestCaseFiles",
                column: "ImportedTestCaseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataValues");

            migrationBuilder.DropTable(
                name: "DataRows");

            migrationBuilder.DropTable(
                name: "DataEntities");

            migrationBuilder.DropTable(
                name: "TestCaseFiles");

            migrationBuilder.DropTable(
                name: "ImportedTestCases");
        }
    }
}
