using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartBIST.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingPredictionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CurrentPrice",
                table: "AIStockPredictions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "DataPoints",
                table: "AIStockPredictions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LastCloseDate",
                table: "AIStockPredictions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PercentChange",
                table: "AIStockPredictions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PredictedPrice",
                table: "AIStockPredictions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PredictionDate",
                table: "AIStockPredictions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PriceChange",
                table: "AIStockPredictions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentPrice",
                table: "AIStockPredictions");

            migrationBuilder.DropColumn(
                name: "DataPoints",
                table: "AIStockPredictions");

            migrationBuilder.DropColumn(
                name: "LastCloseDate",
                table: "AIStockPredictions");

            migrationBuilder.DropColumn(
                name: "PercentChange",
                table: "AIStockPredictions");

            migrationBuilder.DropColumn(
                name: "PredictedPrice",
                table: "AIStockPredictions");

            migrationBuilder.DropColumn(
                name: "PredictionDate",
                table: "AIStockPredictions");

            migrationBuilder.DropColumn(
                name: "PriceChange",
                table: "AIStockPredictions");
        }
    }
}
