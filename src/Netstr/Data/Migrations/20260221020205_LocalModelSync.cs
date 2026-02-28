using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Netstr.Data.Migrations
{
    /// <inheritdoc />
    public partial class LocalModelSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EventJson",
                table: "Events",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EventJson",
                table: "Events");
        }
    }
}
