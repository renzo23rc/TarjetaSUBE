using Microsoft.EntityFrameworkCore;

namespace TarjetaSUBE;

public class TarjetaDbContext : DbContext
{
    public TarjetaDbContext() { }

    public TarjetaDbContext(DbContextOptions<TarjetaDbContext> opciones) : base(opciones) { }

    public DbSet<Tarjeta> Tarjetas => Set<Tarjeta>();
    public DbSet<Colectivo> Colectivos => Set<Colectivo>();
    public DbSet<Boleto> Boletos => Set<Boleto>();
    public DbSet<TarjetaTipo> TarjetaTipos => Set<TarjetaTipo>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite("Data Source=tarjetasube.db");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tarjeta>()
            .Property(t => t.Saldo)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Tarjeta>()
            .HasOne(t => t.TarjetaTipo)
            .WithMany()
            .HasForeignKey(t => t.TarjetaTipoId);

        modelBuilder.Entity<TarjetaTipo>().HasData(
            new TarjetaTipo { Id = 1, Nombre = "Normal", PorcentajeDescuento = 0m },
            new TarjetaTipo { Id = 2, Nombre = "Medio boleto estudiantil", PorcentajeDescuento = 50m },
            new TarjetaTipo { Id = 3, Nombre = "Boleto gratuito estudiantil", PorcentajeDescuento = 100m },
            new TarjetaTipo { Id = 4, Nombre = "Franquicia completa", PorcentajeDescuento = 100m });

        modelBuilder.Entity<Boleto>()
            .Property(b => b.Tarifa)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Boleto>()
            .Property(b => b.SaldoRestante)
            .HasPrecision(18, 2);
    }
}
