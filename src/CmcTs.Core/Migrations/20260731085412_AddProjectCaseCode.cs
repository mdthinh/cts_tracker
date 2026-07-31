using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CmcTs.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectCaseCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CaseCode",
                table: "Projects",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CaseCode",
                table: "Projects");
        }
    }
}
