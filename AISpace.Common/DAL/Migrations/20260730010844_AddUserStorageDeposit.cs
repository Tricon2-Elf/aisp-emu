using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISpace.Common.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddUserStorageDeposit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "StorageDeposit",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StorageDeposit",
                table: "Users");
        }
    }
}
