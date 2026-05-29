using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Web.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameAuditFieldsAndMapBaseAuditableEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReportImageUploads_Users_CreatedByUserId",
                table: "ReportImageUploads");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Users_CreatedByUserId",
                table: "Reports");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "Users",
                newName: "Created");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "Reports",
                newName: "LastModified");

            migrationBuilder.RenameColumn(
                name: "CreatedByUserId",
                table: "Reports",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "Reports",
                newName: "Created");

            migrationBuilder.RenameIndex(
                name: "IX_Reports_CreatedByUserId",
                table: "Reports",
                newName: "IX_Reports_CreatedBy");

            migrationBuilder.RenameColumn(
                name: "CreatedByUserId",
                table: "ReportImageUploads",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "ReportImageUploads",
                newName: "Created");

            migrationBuilder.RenameIndex(
                name: "IX_ReportImageUploads_CreatedByUserId",
                table: "ReportImageUploads",
                newName: "IX_ReportImageUploads_CreatedBy");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "ReportConfirmations",
                newName: "Created");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastModified",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastModified",
                table: "ReportImageUploads",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "ReportConfirmations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastModified",
                table: "ReportConfirmations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ReportImageUploads_Users_CreatedBy",
                table: "ReportImageUploads",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Users_CreatedBy",
                table: "Reports",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReportImageUploads_Users_CreatedBy",
                table: "ReportImageUploads");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Users_CreatedBy",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastModified",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastModified",
                table: "ReportImageUploads");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ReportConfirmations");

            migrationBuilder.DropColumn(
                name: "LastModified",
                table: "ReportConfirmations");

            migrationBuilder.RenameColumn(
                name: "Created",
                table: "Users",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "LastModified",
                table: "Reports",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "Reports",
                newName: "CreatedByUserId");

            migrationBuilder.RenameColumn(
                name: "Created",
                table: "Reports",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_Reports_CreatedBy",
                table: "Reports",
                newName: "IX_Reports_CreatedByUserId");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "ReportImageUploads",
                newName: "CreatedByUserId");

            migrationBuilder.RenameColumn(
                name: "Created",
                table: "ReportImageUploads",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_ReportImageUploads_CreatedBy",
                table: "ReportImageUploads",
                newName: "IX_ReportImageUploads_CreatedByUserId");

            migrationBuilder.RenameColumn(
                name: "Created",
                table: "ReportConfirmations",
                newName: "CreatedAtUtc");

            migrationBuilder.AddForeignKey(
                name: "FK_ReportImageUploads_Users_CreatedByUserId",
                table: "ReportImageUploads",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Users_CreatedByUserId",
                table: "Reports",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
