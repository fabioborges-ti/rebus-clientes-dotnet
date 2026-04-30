using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rebus.Clientes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameClienteCriadoEmUtcToCriadoEm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CriadoEmUtc",
                table: "clientes",
                newName: "CriadoEm");

            migrationBuilder.RenameColumn(
                name: "CriadoEmUtc",
                table: "cliente_operacoes",
                newName: "CriadoEm");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CriadoEm",
                table: "clientes",
                newName: "CriadoEmUtc");

            migrationBuilder.RenameColumn(
                name: "CriadoEm",
                table: "cliente_operacoes",
                newName: "CriadoEmUtc");
        }
    }
}
