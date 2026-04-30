using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rebus.Clientes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClienteOperacoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cliente_operacoes",
                columns: table => new
                {
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    ClienteId = table.Column<Guid>(type: "uuid", nullable: true),
                    MensagemErro = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CriadoEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEmUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cliente_operacoes", x => x.CorrelationId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cliente_operacoes");
        }
    }
}
