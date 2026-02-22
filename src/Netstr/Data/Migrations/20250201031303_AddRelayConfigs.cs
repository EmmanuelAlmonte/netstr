using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Netstr.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRelayConfigs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RelayConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PubKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RelayUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Read = table.Column<bool>(type: "boolean", nullable: false),
                    Write = table.Column<bool>(type: "boolean", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelayConfigs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RelayConfigs_PubKey_RelayUrl",
                table: "RelayConfigs",
                columns: new[] { "PubKey", "RelayUrl" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RelayConfigs");
        }
    }
}
