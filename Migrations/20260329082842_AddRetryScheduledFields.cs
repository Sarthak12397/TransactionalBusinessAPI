using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransactionalBusinessApi.Migrations
{
    /// <inheritdoc />
    public partial class AddRetryScheduledFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                table: "Transactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastRetriedAt",
                table: "Transactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxRetries",
                table: "Transactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextRetryAt",
                table: "Transactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "Transactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailureReason",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "LastRetriedAt",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "MaxRetries",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "NextRetryAt",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "Transactions");
        }
    }
}
