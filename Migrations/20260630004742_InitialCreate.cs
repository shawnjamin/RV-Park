using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RVPark.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RvSites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SiteNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    MaxRvLength = table.Column<int>(type: "INTEGER", nullable: false),
                    NightlyRate = table.Column<decimal>(type: "TEXT", precision: 8, scale: 2, nullable: false),
                    HookupType = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    IsAvailable = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RvSites", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RvSites_SiteNumber",
                table: "RvSites",
                column: "SiteNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RvSites");
        }
    }
}
