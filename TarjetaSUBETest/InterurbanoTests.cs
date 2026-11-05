using Microsoft.EntityFrameworkCore;
using TarjetaSUBE;

namespace TarjetaSUBETest;

public class InterurbanoTests
{
    private TarjetaDbContext _db = null!;
    private FakeTimeProvider _reloj = null!;

    [SetUp]
    public void Setup()
    {
        var opciones = new DbContextOptionsBuilder<TarjetaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new TarjetaDbContext(opciones);
        Contexto.Db = _db;

        // Lunes 16/03/2026 10:00 (dentro de la franja de franquicias).
        _reloj = new FakeTimeProvider(new DateTime(2026, 3, 16, 10, 0, 0));
        Contexto.Reloj = _reloj;
    }

    [TearDown]
    public void TearDown()
    {
        Contexto.Reloj = new ProveedorDeFechaSistema();
        _db.Dispose();
    }

    [Test]
    public void CrearInterurbano_GuardaLaLineaYCobraTarifaInterurbana()
    {
        var colectivo = Colectivo.CrearInterurbano("Gálvez");

        Assert.That(colectivo.Id, Is.Not.EqualTo(0));
        var guardado = _db.Colectivos.AsNoTracking().SingleOrDefault(c => c.Id == colectivo.Id);
        Assert.That(guardado, Is.Not.Null);
        Assert.That(guardado!.Linea, Is.EqualTo("Gálvez"));
        Assert.That(guardado.Tarifa, Is.EqualTo(Colectivo.TarifaInterurbana));
        var tarjeta = Tarjeta.Crear(new TarjetaNormal());
        tarjeta.CargarSaldo(5000m);

        var resultado = colectivo.PagarCon(tarjeta);

        Assert.That(resultado, Is.True);
        Assert.That(tarjeta.Saldo, Is.EqualTo(2000m));
        var boleto = _db.Boletos.Single();
        Assert.That(boleto.Tarifa, Is.EqualTo(3000m));
    }

    [Test]
    public void PagarCon_MedioBoleto_EnInterurbano_CobraLaMitad()
    {
        var tarjeta = Tarjeta.Crear(new TarjetaMedioBoleto());
        tarjeta.CargarSaldo(5000m);

        var resultado = Colectivo.CrearInterurbano("Gálvez").PagarCon(tarjeta);

        Assert.That(resultado, Is.True);
        var boleto = _db.Boletos.Single();
        Assert.That(boleto.Tarifa, Is.EqualTo(1500m));
    }

    [Test]
    public void PagarCon_BoletoGratuito_EnInterurbano_CobraTarifaCompletaDesdeElTercerViaje()
    {
        var tarjeta = Tarjeta.Crear(new TarjetaBoletoGratuito());
        tarjeta.CargarSaldo(5000m);
        var colectivo = Colectivo.CrearInterurbano("Gálvez");

        Assert.That(colectivo.PagarCon(tarjeta), Is.True);
        _reloj.Avanzar(TimeSpan.FromMinutes(10));
        Assert.That(colectivo.PagarCon(tarjeta), Is.True);
        _reloj.Avanzar(TimeSpan.FromMinutes(10));
        Assert.That(colectivo.PagarCon(tarjeta), Is.True);

        var tarifas = _db.Boletos.OrderBy(b => b.Id).Select(b => b.Tarifa).ToArray();
        Assert.That(tarifas, Is.EqualTo(new[] { 0m, 0m, 3000m }));
        Assert.That(tarjeta.Saldo, Is.EqualTo(2000m));
    }

    [Test]
    public void PagarCon_FranquiciaCompleta_EnInterurbano_NoCobra()
    {
        var tarjeta = Tarjeta.Crear(new TarjetaFranquiciaCompleta());
        tarjeta.CargarSaldo(2000m);

        var resultado = Colectivo.CrearInterurbano("Gálvez").PagarCon(tarjeta);

        Assert.That(resultado, Is.True);
        var boleto = _db.Boletos.Single();
        Assert.That(boleto.Tarifa, Is.EqualTo(0m));
    }
}
