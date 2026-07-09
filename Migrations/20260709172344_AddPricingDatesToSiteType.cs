using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RVPark.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingDatesToSiteType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "SiteTypes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "SiteTypes",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "SiteTypes");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "SiteTypes");
        }
    }
}
