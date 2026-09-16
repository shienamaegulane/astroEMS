using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace astroEMS.Migrations
{
    /// <inheritdoc />
    public partial class AddGovernmentContributionTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PagIbigContributionTables",
                columns: table => new
                {
                    PagIbigContributionTableID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagIbigContributionTables", x => x.PagIbigContributionTableID);
                });

            migrationBuilder.CreateTable(
                name: "PhilHealthSettings",
                columns: table => new
                {
                    PhilHealthSettingID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    PremiumRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SalaryFloor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SalaryCeiling = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhilHealthSettings", x => x.PhilHealthSettingID);
                });

            migrationBuilder.CreateTable(
                name: "SSSContributionTables",
                columns: table => new
                {
                    SSSContributionTableID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SSSContributionTables", x => x.SSSContributionTableID);
                });

            migrationBuilder.CreateTable(
                name: "WithholdingTaxBrackets",
                columns: table => new
                {
                    WithholdingTaxBracketID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    MinAnnualIncome = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxAnnualIncome = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    BaseTax = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WithholdingTaxBrackets", x => x.WithholdingTaxBracketID);
                });

            migrationBuilder.CreateTable(
                name: "PagIbigContributionBrackets",
                columns: table => new
                {
                    PagIbigContributionBracketID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PagIbigContributionTableID = table.Column<int>(type: "int", nullable: false),
                    MinSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    EmployeeRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EmployerRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxCompensation = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagIbigContributionBrackets", x => x.PagIbigContributionBracketID);
                    table.ForeignKey(
                        name: "FK_PagIbigContributionBrackets_PagIbigContributionTables_PagIbigContributionTableID",
                        column: x => x.PagIbigContributionTableID,
                        principalTable: "PagIbigContributionTables",
                        principalColumn: "PagIbigContributionTableID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SSSContributionBrackets",
                columns: table => new
                {
                    SSSContributionBracketID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SSSContributionTableID = table.Column<int>(type: "int", nullable: false),
                    MinSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MonthlySalaryCredit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EmployeeShare = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EmployerShare = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SSSContributionBrackets", x => x.SSSContributionBracketID);
                    table.ForeignKey(
                        name: "FK_SSSContributionBrackets_SSSContributionTables_SSSContributionTableID",
                        column: x => x.SSSContributionTableID,
                        principalTable: "SSSContributionTables",
                        principalColumn: "SSSContributionTableID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PagIbigContributionBrackets_PagIbigContributionTableID",
                table: "PagIbigContributionBrackets",
                column: "PagIbigContributionTableID");

            migrationBuilder.CreateIndex(
                name: "IX_SSSContributionBrackets_SSSContributionTableID",
                table: "SSSContributionBrackets",
                column: "SSSContributionTableID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PagIbigContributionBrackets");

            migrationBuilder.DropTable(
                name: "SSSContributionBrackets");

            migrationBuilder.DropTable(
                name: "PhilHealthSettings");

            migrationBuilder.DropTable(
                name: "WithholdingTaxBrackets");

            migrationBuilder.DropTable(
                name: "PagIbigContributionTables");

            migrationBuilder.DropTable(
                name: "SSSContributionTables");
        }
    }
}