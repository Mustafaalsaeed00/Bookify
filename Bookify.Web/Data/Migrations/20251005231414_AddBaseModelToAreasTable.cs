using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bookify.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBaseModelToAreasTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedById",
                table: "Areas",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "Areas",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Areas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LastUpdatedById",
                table: "Areas",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdatedOn",
                table: "Areas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Areas_CreatedById",
                table: "Areas",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Areas_LastUpdatedById",
                table: "Areas",
                column: "LastUpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Areas_AspNetUsers_CreatedById",
                table: "Areas",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Areas_AspNetUsers_LastUpdatedById",
                table: "Areas",
                column: "LastUpdatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Areas_AspNetUsers_CreatedById",
                table: "Areas");

            migrationBuilder.DropForeignKey(
                name: "FK_Areas_AspNetUsers_LastUpdatedById",
                table: "Areas");

            migrationBuilder.DropIndex(
                name: "IX_Areas_CreatedById",
                table: "Areas");

            migrationBuilder.DropIndex(
                name: "IX_Areas_LastUpdatedById",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "LastUpdatedById",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "LastUpdatedOn",
                table: "Areas");
        }
    }
}
