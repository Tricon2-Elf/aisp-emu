using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using aisp.Common.DAL;

#nullable disable

namespace aisp.Common.DAL.Migrations;

[Migration("20260904000000_AddFriendLinkTags")]
[DbContext(typeof(MainContext))]
public partial class AddFriendLinkTags : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "FriendLinkTags",
            columns: table => new
            {
                CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                Slot = table.Column<uint>(type: "INTEGER", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 61, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FriendLinkTags", x => new { x.CharacterId, x.Slot });
                table.ForeignKey(
                    name: "FK_FriendLinkTags_Characters_CharacterId",
                    column: x => x.CharacterId,
                    principalTable: "Characters",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "FriendLinkTags");
}
