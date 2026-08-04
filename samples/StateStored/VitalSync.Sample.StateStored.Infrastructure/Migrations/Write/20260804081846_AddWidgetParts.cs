using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VitalSync.Sample.StateStored.Infrastructure.Migrations.Write
{
    /// <inheritdoc />
    public partial class AddWidgetParts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "widget_parts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    widget_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_widget_parts", x => x.id);
                    table.ForeignKey(
                        name: "FK_widget_parts_widgets_widget_id",
                        column: x => x.widget_id,
                        principalTable: "widgets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_widget_parts_widget_id",
                table: "widget_parts",
                column: "widget_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "widget_parts");
        }
    }
}
