using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TransactionalBusinessApi.Migrations
{
    /// <inheritdoc />
    public partial class RenameLastRetriedAtToLastAttemptAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastRetriedAt",
                table: "Transactions",
                newName: "LastAttemptAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastAttemptAt",
                table: "Transactions",
                newName: "LastRetriedAt");
        }
    }
}
