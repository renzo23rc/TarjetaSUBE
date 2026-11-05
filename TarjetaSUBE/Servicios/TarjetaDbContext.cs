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
    public DbSet<RetiroBicicleta> RetirosBicicletas => Set<RetiroBicicleta>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite("Data Source=tarjetasube.db");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RetiroBicicleta>()
            .HasOne(r => r.Tarjeta)
            .WithMany()
            .HasForeignKey(r => r.TarjetaId);

        modelBuilder.Entity<RetiroBicicleta>()
            .Property(r => r.MultaPendiente)
            .HasPrecision(18, 2);

        modelBuilder.Entity<RetiroBicicleta>()
            .HasIndex(r => r.TarjetaId, "IX_RetirosBicicletas_TarjetaId_Activo")
            .IsUnique()
            .HasFilter("\"FechaDevolucion\" IS NULL");

        modelBuilder.Entity<Tarjeta>()
            .Property(t => t.Saldo)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Tarjeta>()
            .Property(t => t.SaldoPendiente)
            .HasPrecision(18, 2);

        modelBuilder.Entity<TarjetaTipo>()
            .Property(t => t.PorcentajeDescuento)
            .HasPrecision(5, 2);

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

        modelBuilder.Entity<Colectivo>()
            .Property(c => c.Tarifa)
            .HasPrecision(18, 2);
    }
}
