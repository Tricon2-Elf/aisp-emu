using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aisp.Common.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddRoboProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Like1",
                table: "Robos",
                type: "TEXT",
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<string>(
                name: "Like2",
                table: "Robos",
                type: "TEXT",
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<string>(
                name: "Like3",
                table: "Robos",
                type: "TEXT",
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<string>(
                name: "LikeDesc1",
                table: "Robos",
                type: "TEXT",
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<string>(
                name: "LikeDesc2",
                table: "Robos",
                type: "TEXT",
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<string>(
                name: "LikeDesc3",
                table: "Robos",
                type: "TEXT",
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<string>(
                name: "ProfileDescription",
                table: "Robos",
                type: "TEXT",
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<uint>(
                name: "ProfileUnknownDword04",
                table: "Robos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u
            );

            migrationBuilder.AddColumn<uint>(
                name: "ProfileUnknownDword08",
                table: "Robos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Like1", table: "Robos");

            migrationBuilder.DropColumn(name: "Like2", table: "Robos");

            migrationBuilder.DropColumn(name: "Like3", table: "Robos");

            migrationBuilder.DropColumn(name: "LikeDesc1", table: "Robos");

            migrationBuilder.DropColumn(name: "LikeDesc2", table: "Robos");

            migrationBuilder.DropColumn(name: "LikeDesc3", table: "Robos");

            migrationBuilder.DropColumn(name: "ProfileDescription", table: "Robos");

            migrationBuilder.DropColumn(name: "ProfileUnknownDword04", table: "Robos");

            migrationBuilder.DropColumn(name: "ProfileUnknownDword08", table: "Robos");
        }
    }
}
