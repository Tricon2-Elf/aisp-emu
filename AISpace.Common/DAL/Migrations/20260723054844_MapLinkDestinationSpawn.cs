using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISpace.Common.DAL.Migrations
{
    /// <inheritdoc />
    public partial class MapLinkDestinationSpawn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DestinationSpawnRotation",
                table: "MapLinks",
                type: "INTEGER",
                nullable: true
            );

            migrationBuilder.AddColumn<float>(
                name: "DestinationSpawnX",
                table: "MapLinks",
                type: "REAL",
                nullable: true
            );

            migrationBuilder.AddColumn<float>(
                name: "DestinationSpawnY",
                table: "MapLinks",
                type: "REAL",
                nullable: true
            );

            migrationBuilder.AddColumn<float>(
                name: "DestinationSpawnZ",
                table: "MapLinks",
                type: "REAL",
                nullable: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "DestinationSpawnRotation", table: "MapLinks");

            migrationBuilder.DropColumn(name: "DestinationSpawnX", table: "MapLinks");

            migrationBuilder.DropColumn(name: "DestinationSpawnY", table: "MapLinks");

            migrationBuilder.DropColumn(name: "DestinationSpawnZ", table: "MapLinks");
        }
    }
}
