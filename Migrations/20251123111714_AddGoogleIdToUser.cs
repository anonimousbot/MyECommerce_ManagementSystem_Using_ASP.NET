using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleIdToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("ca8e20a9-d615-4157-b94e-e0c76d95a7ff"));

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "GoogleId",
                table: "Users",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "DateCreated", "DateModified", "Description", "Name" },
                values: new object[] { new Guid("d5862b71-9c96-46d9-a8a6-51badbd7a4c1"), new DateTime(2025, 11, 23, 11, 17, 12, 348, DateTimeKind.Utc).AddTicks(697), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Can Order Items from the Store", "Customer" });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("c8f2e5ab-9f34-4b97-8b7c-1a5e86c77e42"),
                column: "GoogleId",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("d5862b71-9c96-46d9-a8a6-51badbd7a4c1"));

            migrationBuilder.DropColumn(
                name: "GoogleId",
                table: "Users");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "PasswordHash",
                keyValue: null,
                column: "PasswordHash",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "DateCreated", "DateModified", "Description", "Name" },
                values: new object[] { new Guid("ca8e20a9-d615-4157-b94e-e0c76d95a7ff"), new DateTime(2025, 11, 20, 16, 20, 33, 294, DateTimeKind.Utc).AddTicks(5359), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Can Order Items from the Store", "Customer" });
        }
    }
}
