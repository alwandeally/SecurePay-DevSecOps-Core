using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecurePay.Transactions.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionLifecycleAuditEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "transaction_lifecycle_audit_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    action = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    resulting_status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transaction_lifecycle_audit_entries", x => x.id);
                    table.CheckConstraint("ck_transaction_lifecycle_audit_action_status", "(action = 'Complete' AND resulting_status = 'Completed')\r\nOR (action = 'Fail' AND resulting_status = 'Failed')");
                    table.ForeignKey(
                        name: "FK_transaction_lifecycle_audit_entries_payment_transactions_tr~",
                        column: x => x.transaction_id,
                        principalTable: "payment_transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_transaction_lifecycle_audit_transaction_id",
                table: "transaction_lifecycle_audit_entries",
                column: "transaction_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "transaction_lifecycle_audit_entries");
        }
    }
}
