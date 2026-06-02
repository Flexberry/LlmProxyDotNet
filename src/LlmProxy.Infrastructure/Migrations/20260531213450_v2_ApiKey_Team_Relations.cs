using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LlmProxy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v2_ApiKey_Team_Relations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BudgetId",
                table: "ApiKeys",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RateLimitConfigJson",
                table: "ApiKeys",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TeamId",
                table: "ApiKeys",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_BudgetId",
                table: "ApiKeys",
                column: "BudgetId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_TeamId",
                table: "ApiKeys",
                column: "TeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_ApiKeys_Budgets_BudgetId",
                table: "ApiKeys",
                column: "BudgetId",
                principalTable: "Budgets",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ApiKeys_Teams_TeamId",
                table: "ApiKeys",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApiKeys_Budgets_BudgetId",
                table: "ApiKeys");

            migrationBuilder.DropForeignKey(
                name: "FK_ApiKeys_Teams_TeamId",
                table: "ApiKeys");

            migrationBuilder.DropIndex(
                name: "IX_ApiKeys_BudgetId",
                table: "ApiKeys");

            migrationBuilder.DropIndex(
                name: "IX_ApiKeys_TeamId",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "BudgetId",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "RateLimitConfigJson",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "TeamId",
                table: "ApiKeys");
        }
    }
}
