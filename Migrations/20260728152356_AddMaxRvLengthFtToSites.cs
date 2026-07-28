using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RVPark.Migrations
{
    /// <inheritdoc />
    public partial class AddMaxRvLengthFtToSites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxRvLengthFt",
                table: "Sites",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxRvLengthFt",
                table: "Sites");
        }
    }
}
