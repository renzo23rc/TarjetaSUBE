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

        modelBuilder.Entity<TarjetaNormal>().HasData(new TarjetaNormal { Id = 1 });
        modelBuilder.Entity<TarjetaMedioBoleto>().HasData(new TarjetaMedioBoleto { Id = 2 });
        modelBuilder.Entity<TarjetaBoletoGratuito>().HasData(new TarjetaBoletoGratuito { Id = 3 });
        modelBuilder.Entity<TarjetaFranquiciaCompleta>().HasData(new TarjetaFranquiciaCompleta { Id = 4 });

        modelBuilder.Entity<TarjetaTipo>().UseTphMappingStrategy();
        modelBuilder.Entity<TarjetaNormal>();
        modelBuilder.Entity<TarjetaMedioBoleto>();
        modelBuilder.Entity<TarjetaBoletoGratuito>();
        modelBuilder.Entity<TarjetaFranquiciaCompleta>();

        modelBuilder.Entity<Boleto>()
            .Property(b => b.Tarifa)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Boleto>()
            .Property(b => b.TotalAbonado)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Boleto>()
            .Property(b => b.SaldoRestante)
            .HasPrecision(18, 2);
    }
}
