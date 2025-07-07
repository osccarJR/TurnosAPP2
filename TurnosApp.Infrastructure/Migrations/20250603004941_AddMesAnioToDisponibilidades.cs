using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TurnosApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMesAnioToDisponibilidades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Anio",
                table: "Disponibilidades",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Mes",
                table: "Disponibilidades",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Anio",
                table: "Disponibilidades");

            migrationBuilder.DropColumn(
                name: "Mes",
                table: "Disponibilidades");
        }
    }
}
