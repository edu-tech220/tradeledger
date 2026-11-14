using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradeLedger.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLotConcurrencyToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Lots",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "Lots");
        }
    }
}
