using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.AccountNumber)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.HasOne<BankAccount>()
            .WithMany()
            .HasForeignKey(x => x.AccountNumber)
            .HasPrincipalKey(x => x.AccountNumber)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.AccountNumber);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => new { x.AccountNumber, x.Type, x.CreatedAt });
    }
}
