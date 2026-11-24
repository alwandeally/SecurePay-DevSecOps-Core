using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecurePay.Transactions.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationsTransactionQueueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_payment_transactions_created_at_utc",
                table: "payment_transactions",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_payment_transactions_status_created_at_utc",
                table: "payment_transactions",
                columns: new[] { "status", "created_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_payment_transactions_created_at_utc",
                table: "payment_transactions");

            migrationBuilder.DropIndex(
                name: "ix_payment_transactions_status_created_at_utc",
                table: "payment_transactions");
        }
    }
}
