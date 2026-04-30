using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rebus.Clientes.Domain.Entities;

namespace Rebus.Clientes.Infrastructure.Persistence.Configurations;

public class ClienteOperacaoConfiguration : IEntityTypeConfiguration<ClienteOperacao>
{
    public void Configure(EntityTypeBuilder<ClienteOperacao> builder)
    {
        builder.ToTable("cliente_operacoes");

        builder.HasKey(x => x.CorrelationId);

        builder.Property(x => x.Tipo)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Estado)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.MensagemErro)
            .HasMaxLength(2000);

        builder.Property(x => x.CriadoEm)
            .IsRequired();

        builder.Property(x => x.AtualizadoEmUtc)
            .IsRequired();
    }
}
