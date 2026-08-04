using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VitalSync.Sample.StateStored.Infrastructure.Migrations.Read
{
    /// <inheritdoc />
    public partial class AddWidgetPartCounters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "part_count",
                table: "widgets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "total_quantity",
                table: "widgets",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "part_count",
                table: "widgets");

            migrationBuilder.DropColumn(
                name: "total_quantity",
                table: "widgets");
        }
    }
}
