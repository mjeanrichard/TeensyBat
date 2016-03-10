using System;
using System.Collections.Generic;
using Microsoft.Data.Entity.Migrations;

namespace TeensyBatMap.Migrations
{
    public partial class Migration02FftData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "FftData",
                table: "BatCalls",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "FftData", table: "BatCalls");
        }
    }
}
