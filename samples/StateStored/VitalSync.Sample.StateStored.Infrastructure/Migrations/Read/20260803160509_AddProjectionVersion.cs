using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VitalSync.Sample.StateStored.Infrastructure.Migrations.Read
{
    /// <inheritdoc />
    public partial class AddProjectionVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "version",
                table: "widgets",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "version",
                table: "widgets");
        }
    }
}
