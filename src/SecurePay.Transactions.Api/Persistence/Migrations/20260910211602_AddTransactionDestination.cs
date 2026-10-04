using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecurePay.Transactions.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionDestination : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "destination_user_id",
                table: "payment_transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_transactions_destination_user_id_created_at_utc",
                table: "payment_transactions",
                columns: new[] { "destination_user_id", "created_at_utc" },
                filter: "destination_user_id IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_payment_transactions_destination",
                table: "payment_transactions",
                sql: "(\r\n    transaction_type = 'Transfer'\r\n    AND destination_user_id IS NOT NULL\r\n    AND destination_user_id <> user_id\r\n)\r\nOR\r\n(\r\n    transaction_type IN ('Deposit', 'Withdrawal')\r\n    AND destination_user_id IS NULL\r\n)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_payment_transactions_destination_user_id_created_at_utc",
                table: "payment_transactions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_payment_transactions_destination",
                table: "payment_transactions");

            migrationBuilder.DropColumn(
                name: "destination_user_id",
                table: "payment_transactions");
        }
    }
}
