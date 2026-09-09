using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecurePay.Transactions.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDoubleEntryLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ledger_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    wallet_id = table.Column<Guid>(type: "uuid", nullable: true),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ledger_accounts", x => x.id);
                    table.CheckConstraint("ck_ledger_accounts_type_link", "(account_type = 'CustomerWallet' AND wallet_id IS NOT NULL)\r\nOR\r\n(account_type = 'PlatformClearing' AND wallet_id IS NULL)");
                    table.ForeignKey(
                        name: "FK_ledger_accounts_wallet_accounts_wallet_id",
                        column: x => x.wallet_id,
                        principalTable: "wallet_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ledger_postings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ledger_postings", x => x.id);
                    table.ForeignKey(
                        name: "FK_ledger_postings_payment_transactions_payment_transaction_id",
                        column: x => x.payment_transaction_id,
                        principalTable: "payment_transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ledger_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ledger_posting_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ledger_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    direction = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ledger_entries", x => x.id);
                    table.CheckConstraint("ck_ledger_entries_amount_positive", "amount > 0");
                    table.CheckConstraint("ck_ledger_entries_direction", "direction IN ('Debit', 'Credit')");
                    table.ForeignKey(
                        name: "FK_ledger_entries_ledger_accounts_ledger_account_id",
                        column: x => x.ledger_account_id,
                        principalTable: "ledger_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ledger_entries_ledger_postings_ledger_posting_id",
                        column: x => x.ledger_posting_id,
                        principalTable: "ledger_postings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_ledger_accounts_platform_type_currency",
                table: "ledger_accounts",
                columns: new[] { "account_type", "currency" },
                unique: true,
                filter: "wallet_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_ledger_accounts_wallet_id",
                table: "ledger_accounts",
                column: "wallet_id",
                unique: true,
                filter: "wallet_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_ledger_entries_account_id",
                table: "ledger_entries",
                column: "ledger_account_id");

            migrationBuilder.CreateIndex(
                name: "ux_ledger_entries_posting_account",
                table: "ledger_entries",
                columns: new[] { "ledger_posting_id", "ledger_account_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_ledger_postings_payment_transaction_id",
                table: "ledger_postings",
                column: "payment_transaction_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ledger_entries");

            migrationBuilder.DropTable(
                name: "ledger_accounts");

            migrationBuilder.DropTable(
                name: "ledger_postings");
        }
    }
}
