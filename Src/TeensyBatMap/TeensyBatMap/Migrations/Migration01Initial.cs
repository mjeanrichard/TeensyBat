using System;
using System.Collections.Generic;
using Microsoft.Data.Entity.Migrations;

namespace TeensyBatMap.Migrations
{
    public partial class Migration01Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BatCalls",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AvgFrequency = table.Column<int>(nullable: false),
                    AvgIntensity = table.Column<int>(nullable: false),
                    BatNodeLogId = table.Column<int>(nullable: false),
                    Duration = table.Column<int>(nullable: false),
                    MaxFrequency = table.Column<int>(nullable: false),
                    MaxIntensity = table.Column<int>(nullable: false),
                    StartTime = table.Column<DateTime>(nullable: false),
                    StartTimeMs = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatCall", x => x.Id);
                });
            migrationBuilder.CreateTable(
                name: "BatNodeLogs",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CallCount = table.Column<int>(nullable: false),
                    Description = table.Column<string>(nullable: true),
                    FirstCallId = table.Column<int>(nullable: true),
                    LastCallId = table.Column<int>(nullable: true),
                    Latitude = table.Column<double>(nullable: false),
                    LogStart = table.Column<DateTime>(nullable: false),
                    Longitude = table.Column<double>(nullable: false),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatNodeLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BatNodeLog_BatCall_FirstCallId",
                        column: x => x.FirstCallId,
                        principalTable: "BatCalls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BatNodeLog_BatCall_LastCallId",
                        column: x => x.LastCallId,
                        principalTable: "BatCalls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });
            migrationBuilder.AddForeignKey(
                name: "FK_BatCall_BatNodeLog_BatNodeLogId",
                table: "BatCalls",
                column: "BatNodeLogId",
                principalTable: "BatNodeLogs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_BatNodeLog_BatCall_FirstCallId", table: "BatNodeLogs");
            migrationBuilder.DropForeignKey(name: "FK_BatNodeLog_BatCall_LastCallId", table: "BatNodeLogs");
            migrationBuilder.DropTable("BatCalls");
            migrationBuilder.DropTable("BatNodeLogs");
        }
    }
}
