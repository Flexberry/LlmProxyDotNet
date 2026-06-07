using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LlmProxy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class v1_Update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_RequestLogs",
                table: "RequestLogs");

            migrationBuilder.RenameTable(
                name: "RequestLogs",
                newName: "request_logs");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "request_logs",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "request_logs",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "TokensPrompt",
                table: "request_logs",
                newName: "tokens_prompt");

            migrationBuilder.RenameColumn(
                name: "TokensCompletion",
                table: "request_logs",
                newName: "tokens_completion");

            migrationBuilder.RenameColumn(
                name: "ProviderName",
                table: "request_logs",
                newName: "provider_name");

            migrationBuilder.RenameColumn(
                name: "ModelUsed",
                table: "request_logs",
                newName: "model_used");

            migrationBuilder.RenameColumn(
                name: "ModelRequested",
                table: "request_logs",
                newName: "model_requested");

            migrationBuilder.RenameColumn(
                name: "LatencyMs",
                table: "request_logs",
                newName: "latency_ms");

            migrationBuilder.RenameColumn(
                name: "ErrorMessage",
                table: "request_logs",
                newName: "error_message");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "request_logs",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "ApiKeyHash",
                table: "request_logs",
                newName: "api_key_hash");

            migrationBuilder.RenameIndex(
                name: "IX_RequestLogs_CreatedAt",
                table: "request_logs",
                newName: "IX_request_logs_created_at");

            migrationBuilder.RenameIndex(
                name: "IX_RequestLogs_ApiKeyHash",
                table: "request_logs",
                newName: "IX_request_logs_api_key_hash");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "request_logs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "request_logs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.Sql(@"UPDATE request_logs SET status = LEFT(status, 20) WHERE LENGTH(status) > 20");

            migrationBuilder.AlterColumn<string>(
                name: "provider_name",
                table: "request_logs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.Sql(@"UPDATE request_logs SET provider_name = LEFT(provider_name, 50) WHERE LENGTH(provider_name) > 50");

            migrationBuilder.AlterColumn<string>(
                name: "model_used",
                table: "request_logs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.Sql(@"UPDATE request_logs SET model_used = LEFT(model_used, 200) WHERE LENGTH(model_used) > 200");

            migrationBuilder.AlterColumn<string>(
                name: "model_requested",
                table: "request_logs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.Sql(@"UPDATE request_logs SET model_requested = LEFT(model_requested, 200) WHERE LENGTH(model_requested) > 200");

            migrationBuilder.AlterColumn<string>(
                name: "error_message",
                table: "request_logs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.Sql(@"UPDATE request_logs SET error_message = LEFT(error_message, 500) WHERE LENGTH(error_message) > 500");
            migrationBuilder.AlterColumn<string>(
                name: "api_key_hash",
                table: "request_logs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.Sql(@"UPDATE request_logs SET api_key_hash = LEFT(api_key_hash, 64) WHERE LENGTH(api_key_hash) > 64");

            migrationBuilder.AddColumn<bool>(
                name: "is_streaming",
                table: "request_logs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "response_id",
                table: "request_logs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "request_logs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_request_logs",
                table: "request_logs",
                column: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_request_logs",
                table: "request_logs");

            migrationBuilder.DropColumn(
                name: "is_streaming",
                table: "request_logs");

            migrationBuilder.DropColumn(
                name: "response_id",
                table: "request_logs");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "request_logs");

            migrationBuilder.RenameTable(
                name: "request_logs",
                newName: "RequestLogs");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "RequestLogs",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "RequestLogs",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "tokens_prompt",
                table: "RequestLogs",
                newName: "TokensPrompt");

            migrationBuilder.RenameColumn(
                name: "tokens_completion",
                table: "RequestLogs",
                newName: "TokensCompletion");

            migrationBuilder.RenameColumn(
                name: "provider_name",
                table: "RequestLogs",
                newName: "ProviderName");

            migrationBuilder.RenameColumn(
                name: "model_used",
                table: "RequestLogs",
                newName: "ModelUsed");

            migrationBuilder.RenameColumn(
                name: "model_requested",
                table: "RequestLogs",
                newName: "ModelRequested");

            migrationBuilder.RenameColumn(
                name: "latency_ms",
                table: "RequestLogs",
                newName: "LatencyMs");

            migrationBuilder.RenameColumn(
                name: "error_message",
                table: "RequestLogs",
                newName: "ErrorMessage");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "RequestLogs",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "api_key_hash",
                table: "RequestLogs",
                newName: "ApiKeyHash");

            migrationBuilder.RenameIndex(
                name: "IX_request_logs_created_at",
                table: "RequestLogs",
                newName: "IX_RequestLogs_CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_request_logs_api_key_hash",
                table: "RequestLogs",
                newName: "IX_RequestLogs_ApiKeyHash");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "RequestLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "ProviderName",
                table: "RequestLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "ModelUsed",
                table: "RequestLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "ModelRequested",
                table: "RequestLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "ErrorMessage",
                table: "RequestLogs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ApiKeyHash",
                table: "RequestLogs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AddPrimaryKey(
                name: "PK_RequestLogs",
                table: "RequestLogs",
                column: "Id");
        }
    }
}
