using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace aisp.Common.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddReportTicketChatMessageToxicity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Toxicity",
                table: "ReportTicketChatMessages",
                type: "INTEGER",
                nullable: false,
                defaultValue: false
            );

            migrationBuilder.AddColumn<string>(
                name: "ToxicityReason",
                table: "ReportTicketChatMessages",
                type: "TEXT",
                maxLength: 1024,
                nullable: false,
                defaultValue: ""
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Toxicity", table: "ReportTicketChatMessages");

            migrationBuilder.DropColumn(name: "ToxicityReason", table: "ReportTicketChatMessages");
        }
    }
}
