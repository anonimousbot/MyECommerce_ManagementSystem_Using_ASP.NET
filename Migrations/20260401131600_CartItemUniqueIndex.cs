using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Migrations
{
    /// <inheritdoc />
    public partial class CartItemUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("a4154ee1-7429-4ee7-9f64-ab1e63742487"));

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "DateCreated", "DateModified", "Description", "Name" },
                values: new object[] { new Guid("67cb1ae1-91ab-480f-a236-d7f676f7ab99"), new DateTime(2026, 4, 1, 13, 15, 59, 214, DateTimeKind.Utc).AddTicks(1295), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Can Order Items from the Store", "Customer" });

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId_ItemId",
                table: "CartItems",
                columns: new[] { "CartId", "ItemId" },
                unique: true);

            // Drop the old index only after the new one exists (MySQL requires an index for the FK).
            migrationBuilder.DropIndex(
                name: "IX_CartItems_CartId",
                table: "CartItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("67cb1ae1-91ab-480f-a236-d7f676f7ab99"));

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "DateCreated", "DateModified", "Description", "Name" },
                values: new object[] { new Guid("a4154ee1-7429-4ee7-9f64-ab1e63742487"), new DateTime(2026, 4, 1, 13, 13, 15, 901, DateTimeKind.Utc).AddTicks(1037), new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Can Order Items from the Store", "Customer" });

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartId",
                table: "CartItems",
                column: "CartId");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_CartId_ItemId",
                table: "CartItems");
        }
    }
}
