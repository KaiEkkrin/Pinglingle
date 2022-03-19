using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Pinglingle.Server.Migrations
{
    public partial class AddDigests : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDigested",
                table: "Samples",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Digests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TargetId = table.Column<long>(type: "bigint", nullable: true),
                    StartTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SampleCount = table.Column<int>(type: "integer", nullable: false),
                    Percentile5 = table.Column<double>(type: "double precision", nullable: false),
                    Percentile50 = table.Column<double>(type: "double precision", nullable: false),
                    Percentile95 = table.Column<double>(type: "double precision", nullable: false),
                    ErrorCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Digests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Digests_Targets_TargetId",
                        column: x => x.TargetId,
                        principalTable: "Targets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Samples_IsDigested",
                table: "Samples",
                column: "IsDigested");

            migrationBuilder.CreateIndex(
                name: "IX_Digests_StartTime",
                table: "Digests",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_Digests_TargetId",
                table: "Digests",
                column: "TargetId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Digests");

            migrationBuilder.DropIndex(
                name: "IX_Samples_IsDigested",
                table: "Samples");

            migrationBuilder.DropColumn(
                name: "IsDigested",
                table: "Samples");
        }
    }
}
