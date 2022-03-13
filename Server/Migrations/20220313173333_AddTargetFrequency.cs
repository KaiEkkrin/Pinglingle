using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pinglingle.Server.Migrations
{
    public partial class AddTargetFrequency : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Frequency",
                table: "Targets",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Frequency",
                table: "Targets");
        }
    }
}
