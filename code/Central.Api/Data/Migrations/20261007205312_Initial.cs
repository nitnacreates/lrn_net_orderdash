using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Central.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Channels",
                columns: table => new
                {
                    Key = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    ScheduleMinutes = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastSyncAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Channels", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    ChannelKey = table.Column<string>(type: "text", nullable: false),
                    OrderNumber = table.Column<string>(type: "text", nullable: false),
                    CustomerRef = table.Column<string>(type: "text", nullable: true),
                    OrderDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RequiredDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Customer_Name = table.Column<string>(type: "text", nullable: false),
                    Customer_DeliveryAddress_Line1 = table.Column<string>(type: "text", nullable: true),
                    Customer_DeliveryAddress_Line2 = table.Column<string>(type: "text", nullable: true),
                    Customer_DeliveryAddress_Line3 = table.Column<string>(type: "text", nullable: true),
                    Customer_DeliveryAddress_Line4 = table.Column<string>(type: "text", nullable: true),
                    Customer_DeliveryAddress_Town = table.Column<string>(type: "text", nullable: true),
                    Customer_DeliveryAddress_County = table.Column<string>(type: "text", nullable: true),
                    Customer_DeliveryAddress_Country = table.Column<string>(type: "text", nullable: true),
                    Customer_DeliveryAddress_Postcode = table.Column<string>(type: "text", nullable: true),
                    Customer_Phone = table.Column<string>(type: "text", nullable: true),
                    Customer_Email = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ImportedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DispatchedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => new { x.ChannelKey, x.OrderNumber });
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Sku = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Variant = table.Column<string>(type: "text", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Sku);
                });

            migrationBuilder.CreateTable(
                name: "SyncLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChannelKey = table.Column<string>(type: "text", nullable: false),
                    Direction = table.Column<string>(type: "text", nullable: false),
                    Level = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CanonicalOrderLine",
                columns: table => new
                {
                    CanonicalOrderChannelKey = table.Column<string>(type: "text", nullable: false),
                    CanonicalOrderOrderNumber = table.Column<string>(type: "text", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Sku = table.Column<string>(type: "text", nullable: false),
                    LineRef = table.Column<string>(type: "text", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    TaxCode = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CanonicalOrderLine", x => new { x.CanonicalOrderChannelKey, x.CanonicalOrderOrderNumber, x.Id });
                    table.ForeignKey(
                        name: "FK_CanonicalOrderLine_Orders_CanonicalOrderChannelKey_Canonica~",
                        columns: x => new { x.CanonicalOrderChannelKey, x.CanonicalOrderOrderNumber },
                        principalTable: "Orders",
                        principalColumns: new[] { "ChannelKey", "OrderNumber" },
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CanonicalOrderLine");

            migrationBuilder.DropTable(
                name: "Channels");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "SyncLogs");

            migrationBuilder.DropTable(
                name: "Orders");
        }
    }
}
