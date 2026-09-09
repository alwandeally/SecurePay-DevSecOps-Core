using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecurePay.Transactions.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionConcurrencyToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "payment_transactions",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                table: "payment_transactions");
        }
    }
}
