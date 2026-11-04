using Microsoft.EntityFrameworkCore;
using TarjetaSUBE;

namespace TarjetaSUBETest;

// Interpretación de los límites:
// - Franja horaria: lunes a viernes, de 6:00 (inclusive) a 22:00 (exclusivo).
// - Aplica a todas las franquicias (todas las tarjetas que no son Normal).
public class FranquiciaHorariaTests
{
    private TarjetaDbContext _db = null!;
    private FakeTimeProvider _reloj = null!;

    private static readonly Type[] TiposFranquicia =
    {
        typeof(TarjetaMedioBoleto),
        typeof(TarjetaBoletoGratuito),
        typeof(TarjetaFranquiciaCompleta),
    };

    [SetUp]
    public void Setup()
    {
        var opciones = new DbContextOptionsBuilder<TarjetaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new TarjetaDbContext(opciones);
        Contexto.Db = _db;

        _reloj = new FakeTimeProvider(new DateTime(2026, 3, 16, 10, 0, 0));
        Contexto.Reloj = _reloj;
    }

    [TearDown]
    public void TearDown()
    {
        Contexto.Reloj = TimeProvider.System;
        _db.Dispose();
    }

    [TestCaseSource(nameof(TiposFranquicia))]
    public void Franquicia_EnHorarioHabil_PuedeViajar(Type tipo)
    {
        _reloj.Establecer(new DateTime(2026, 3, 16, 10, 0, 0));
        var tarjeta = CrearTarjeta(tipo);

        Assert.That(Colectivo.Crear("102").PagarCon(tarjeta), Is.True);
    }

    [TestCaseSource(nameof(TiposFranquicia))]
    public void Franquicia_AntesDeLas6_NoPuedeViajar(Type tipo)
    {
        _reloj.Establecer(new DateTime(2026, 3, 16, 5, 59, 0));
        var tarjeta = CrearTarjeta(tipo);

        Assert.That(Colectivo.Crear("102").PagarCon(tarjeta), Is.False);
        Assert.That(tarjeta.Saldo, Is.EqualTo(3000m));
        Assert.That(_db.Boletos.ToList(), Is.Empty);
    }

    [TestCaseSource(nameof(TiposFranquicia))]
    public void Franquicia_ALas22_NoPuedeViajar(Type tipo)
    {
        _reloj.Establecer(new DateTime(2026, 3, 16, 22, 0, 0));
        var tarjeta = CrearTarjeta(tipo);

        Assert.That(Colectivo.Crear("102").PagarCon(tarjeta), Is.False);
        Assert.That(_db.Boletos.ToList(), Is.Empty);
    }

    [TestCaseSource(nameof(TiposFranquicia))]
    public void Franquicia_FinDeSemana_NoPuedeViajar(Type tipo)
    {
        var tarjeta = CrearTarjeta(tipo);

        _reloj.Establecer(new DateTime(2026, 3, 14, 10, 0, 0)); // sábado
        Assert.That(Colectivo.Crear("102").PagarCon(tarjeta), Is.False);

        _reloj.Establecer(new DateTime(2026, 3, 15, 10, 0, 0)); // domingo
        Assert.That(Colectivo.Crear("102").PagarCon(tarjeta), Is.False);

        Assert.That(_db.Boletos.ToList(), Is.Empty);
    }

    [Test]
    public void TarjetaNormal_PuedeViajarFueraDeLaFranja()
    {
        _reloj.Establecer(new DateTime(2026, 3, 15, 23, 0, 0));
        var tarjeta = CrearTarjeta(typeof(TarjetaNormal));

        Assert.That(Colectivo.Crear("102").PagarCon(tarjeta), Is.True);
    }

    private Tarjeta CrearTarjeta(Type tipo)
    {
        var tarjeta = Tarjeta.Crear((TarjetaTipo)Activator.CreateInstance(tipo)!);
        tarjeta.CargarSaldo(3000m);
        return tarjeta;
    }
}
