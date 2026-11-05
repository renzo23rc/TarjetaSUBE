using Microsoft.EntityFrameworkCore;
using TarjetaSUBE;

namespace TarjetaSUBETest;

public class EstacionBicicletasTests
{
    private TarjetaDbContext _db = null!;
    private EstacionBicicletas _estacion = null!;

    [SetUp]
    public void Setup()
    {
        var opciones = new DbContextOptionsBuilder<TarjetaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new TarjetaDbContext(opciones);
        Contexto.Db = _db;
        _estacion = new EstacionBicicletas();
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();
    }

    [Test]
    public void RetirarBici_CobraTarifaDiaria_CuandoHaySaldoSuficienteYSinDemora()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(3000m);

        var resultado = _estacion.RetirarBici(tarjeta);

        Assert.That(resultado, Is.True);
        Assert.That(tarjeta.Saldo, Is.EqualTo(3000m - EstacionBicicletas.TarifaDiaria));
        Assert.That(EstacionBicicletas.TarifaDiaria, Is.EqualTo(2418.75m));
        Assert.That(_estacion.MultaPendiente(tarjeta), Is.EqualTo(0m));
    }

    [Test]
    public void RetirarBici_Rechaza_CuandoNoHaySaldoSuficiente()
    {
        var tarjeta = Tarjeta.Crear();

        var resultado = _estacion.RetirarBici(tarjeta);

        Assert.That(resultado, Is.False);
        Assert.That(tarjeta.Saldo, Is.EqualTo(0m));
    }

    [Test]
    public void RetirarBici_Rechaza_YConservaLaMulta_CuandoHayMultaPendienteYSaldoInsuficiente()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(3000m);
        Assert.That(_estacion.RetirarBici(tarjeta), Is.True);
        Assert.That(_estacion.DevolverBici(tarjeta, TimeSpan.FromMinutes(90)), Is.True);

        var resultado = _estacion.RetirarBici(tarjeta);

        Assert.That(resultado, Is.False);
        Assert.That(tarjeta.Saldo, Is.EqualTo(3000m - EstacionBicicletas.TarifaDiaria));
        Assert.That(_estacion.MultaPendiente(tarjeta), Is.EqualTo(EstacionBicicletas.MultaPorExceso));
    }

    [Test]
    public void RetirarBici_CobraTarifaMasMulta_CuandoHayUnaDevolucionTardia()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(10000m);
        Assert.That(_estacion.RetirarBici(tarjeta), Is.True);
        Assert.That(_estacion.DevolverBici(tarjeta, TimeSpan.FromMinutes(90)), Is.True);

        var costoEsperado = EstacionBicicletas.TarifaDiaria + EstacionBicicletas.MultaPorExceso;
        Assert.That(_estacion.CalcularCosto(tarjeta), Is.EqualTo(costoEsperado));

        var resultado = _estacion.RetirarBici(tarjeta);

        Assert.That(resultado, Is.True);
        Assert.That(tarjeta.Saldo, Is.EqualTo(10000m - EstacionBicicletas.TarifaDiaria - costoEsperado));
        Assert.That(_estacion.MultaPendiente(tarjeta), Is.EqualTo(0m));
    }

    [Test]
    public void RetirarBici_AcumulaLasMultas_CuandoHayVariasDevolucionesTardiasElMismoDia()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(30000m);
        Assert.That(_estacion.RetirarBici(tarjeta), Is.True);
        Assert.That(_estacion.DevolverBici(tarjeta, TimeSpan.FromMinutes(61)), Is.True);
        Assert.That(_estacion.DevolverBici(tarjeta, TimeSpan.FromMinutes(120)), Is.True);

        Assert.That(_estacion.MultaPendiente(tarjeta),
            Is.EqualTo(2m * EstacionBicicletas.MultaPorExceso));

        var costoEsperado = EstacionBicicletas.TarifaDiaria + 2m * EstacionBicicletas.MultaPorExceso;
        var saldoPrevio = tarjeta.Saldo;

        Assert.That(_estacion.RetirarBici(tarjeta), Is.True);
        Assert.That(tarjeta.Saldo, Is.EqualTo(saldoPrevio - costoEsperado));
        Assert.That(_estacion.MultaPendiente(tarjeta), Is.EqualTo(0m));
    }
}
