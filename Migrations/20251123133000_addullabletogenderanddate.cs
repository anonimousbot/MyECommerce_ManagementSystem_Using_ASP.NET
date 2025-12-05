using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Migrations
{
    /// <inheritdoc />
    public partial class addullabletogenderanddate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("d5862b71-9c96-46d9-a8a6-51badbd7a4c1"));

            migrationBuilder.AlterColumn<byte>(
                name: "Gender",
                table: "Customers",
                type: "tinyint unsigned",
                nullable: true,
                oldClrType: typeof(byte),
                oldType: "tinyint unsigned");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateOfBirth",
                table: "Customers",
                type: "datetime(6)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)");

            migrationBuilder.AlterColumn<byte>(
                name: "Gender",
                table: "Admins",
                type: "tinyint unsigned",
                nullable: true,
                oldClrType: typeof(byte),
                oldType: "tinyint unsigned");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateOfBirth",
                table: "Admins",
                type: "datetime(6)",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)");

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "DateCreated", "DateModified", "Description", "Name" },
                values: new object[] { new Guid("37b7677f-4d73-4bad-9cb6-103dfec911b1"), new DateTime(2025, 11, 23, 13, 29, 58, 434, DateTimeKind.Utc).AddTicks(1712), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Can Order Items from the Store", "Customer" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("37b7677f-4d73-4bad-9cb6-103dfec911b1"));

            migrationBuilder.AlterColumn<byte>(
                name: "Gender",
                table: "Customers",
                type: "tinyint unsigned",
                nullable: false,
                defaultValue: (byte)0,
                oldClrType: typeof(byte),
                oldType: "tinyint unsigned",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateOfBirth",
                table: "Customers",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldNullable: true);

            migrationBuilder.AlterColumn<byte>(
                name: "Gender",
                table: "Admins",
                type: "tinyint unsigned",
                nullable: false,
                defaultValue: (byte)0,
                oldClrType: typeof(byte),
                oldType: "tinyint unsigned",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateOfBirth",
                table: "Admins",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime(6)",
                oldNullable: true);

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "DateCreated", "DateModified", "Description", "Name" },
                values: new object[] { new Guid("d5862b71-9c96-46d9-a8a6-51badbd7a4c1"), new DateTime(2025, 11, 23, 11, 17, 12, 348, DateTimeKind.Utc).AddTicks(697), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Can Order Items from the Store", "Customer" });
        }
    }
}
