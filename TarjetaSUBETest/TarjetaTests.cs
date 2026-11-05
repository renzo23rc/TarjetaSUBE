using Microsoft.EntityFrameworkCore;
using TarjetaSUBE;

namespace TarjetaSUBETest;

public class TarjetaTests
{
    private TarjetaDbContext _db = null!;

    [SetUp]
    public void Setup()
    {
        var opciones = new DbContextOptionsBuilder<TarjetaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new TarjetaDbContext(opciones);
        Contexto.Db = _db;
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();
    }

    [Test]
    public void Crear_InicializaSaldoEnCero()
    {
        var tarjeta = Tarjeta.Crear();

        Assert.That(tarjeta.Saldo, Is.EqualTo(0m));
        Assert.That(tarjeta.Id, Is.Not.EqualTo(0));
        Assert.That(_db.Tarjetas.AsNoTracking().SingleOrDefault(t => t.Id == tarjeta.Id), Is.Not.Null);
    }

    [Test]
    public void CargarSaldo_AceptaTodosLosMontosListados()
    {
        decimal[] montosPermitidos =
        {
            2000m, 3000m, 4000m, 5000m, 8000m, 10000m, 15000m, 20000m, 25000m, 30000m
        };

        Assert.That(Tarjeta.MontosDeCarga, Is.EquivalentTo(montosPermitidos));

        foreach (var monto in montosPermitidos)
        {
            var tarjeta = Tarjeta.Crear();
            tarjeta.CargarSaldo(monto);

            Assert.That(tarjeta.Saldo, Is.EqualTo(monto));
        }
    }

    [Test]
    public void CargarSaldo_LanzaExcepcion_CuandoElMontoNoEstaEnLaLista()
    {
        var tarjeta = Tarjeta.Crear();

        Assert.That(
            () => tarjeta.CargarSaldo(1000m),
            Throws.TypeOf<ArgumentException>());

        Assert.That(tarjeta.Saldo, Is.EqualTo(0m));
    }

    [Test]
    public void CargarSaldo_SuperandoMaximoPermitido_AcreditaHasta56000YExcedenteQuedaPendiente()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(30000m);
        tarjeta.CargarSaldo(20000m);

        tarjeta.CargarSaldo(10000m);

        Assert.That(tarjeta.Saldo, Is.EqualTo(56000m));
        Assert.That(tarjeta.SaldoPendiente, Is.EqualTo(4000m));

        var enDb = _db.Tarjetas.AsNoTracking().SingleOrDefault(t => t.Id == tarjeta.Id);
        Assert.That(enDb, Is.Not.Null);
        Assert.That(enDb!.Saldo, Is.EqualTo(56000m));
        Assert.That(enDb.SaldoPendiente, Is.EqualTo(4000m));
    }

    [Test]
    public void Pagar_ConSaldoPendiente_AcreditaAutomaticamenteHastaElMaximoLuegoDelViaje()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(30000m);
        tarjeta.CargarSaldo(20000m);
        tarjeta.CargarSaldo(10000m);
        Assert.That(tarjeta.Saldo, Is.EqualTo(56000m));
        Assert.That(tarjeta.SaldoPendiente, Is.EqualTo(4000m));

        Assert.That(tarjeta.Pagar(2150m), Is.True);

        Assert.That(tarjeta.Saldo, Is.EqualTo(56000m));
        Assert.That(tarjeta.SaldoPendiente, Is.EqualTo(1850m));
    }

    [Test]
    public void AcreditarCarga_ConSaldoNegativoQuePasaAPositivo_RetornaMontoRecuperadoHastaCero()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(2000m);
        tarjeta.Pagar(2500m);
        tarjeta.SaldoPendiente = 2000m;

        var recuperado = tarjeta.AcreditarCarga();

        Assert.That(recuperado, Is.EqualTo(500m));
        Assert.That(tarjeta.Saldo, Is.EqualTo(1500m));
        Assert.That(tarjeta.SaldoPendiente, Is.EqualTo(0m));
    }

    [Test]
    public void AcreditarCarga_ConSaldoNegativoQueSigueEnNegativo_RetornaTotalAcreditado()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(2000m);
        tarjeta.Pagar(3500m);
        tarjeta.SaldoPendiente = 500m;

        var recuperado = tarjeta.AcreditarCarga();

        Assert.That(recuperado, Is.EqualTo(500m));
        Assert.That(tarjeta.Saldo, Is.EqualTo(-1000m));
        Assert.That(tarjeta.SaldoPendiente, Is.EqualTo(0m));
    }

    [Test]
    public void AcreditarCarga_ConSaldoPositivoPrevio_RetornaCero()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(2000m);
        tarjeta.SaldoPendiente = 2000m;

        var recuperado = tarjeta.AcreditarCarga();

        Assert.That(recuperado, Is.EqualTo(0m));
        Assert.That(tarjeta.Saldo, Is.EqualTo(4000m));
        Assert.That(tarjeta.SaldoPendiente, Is.EqualTo(0m));
    }

    [Test]
    public void Pagar_DescuentaLaTarifa()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(3000m);

        var resultado = tarjeta.Pagar(Colectivo.TarifaBasica);

        Assert.That(resultado, Is.True);
        Assert.That(tarjeta.Saldo, Is.EqualTo(850m));
    }

    [Test]
    public void Pagar_RechazaTarifaNegativaSinModificarSaldoNiCargaPendiente()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(30000m);
        tarjeta.CargarSaldo(30000m);

        var resultado = tarjeta.Pagar(-100000m);

        Assert.That(resultado, Is.False);
        Assert.That(tarjeta.Saldo, Is.EqualTo(56000m));
        Assert.That(tarjeta.SaldoPendiente, Is.EqualTo(4000m));
    }

    [Test]
    public void Pagar_PermiteLlegarAlSaldoMinimoPeroNoQuedarPorDebajo()
    {
        var tarjeta = DejarTarjetaConSaldo(150m);

        var primerViaje = tarjeta.Pagar(Colectivo.TarifaBasica);

        Assert.That(primerViaje, Is.True);
        Assert.That(tarjeta.Saldo, Is.EqualTo(-2000m));

        var segundoViaje = tarjeta.Pagar(Colectivo.TarifaBasica);

        Assert.That(segundoViaje, Is.False);
        Assert.That(tarjeta.Saldo, Is.EqualTo(-2000m));
    }

    [Test]
    public void CargarSaldo_ConSaldoNegativoPorViajePlus_DescuentaDeuda()
    {
        var tarjeta = DejarTarjetaConSaldo(200m);

        tarjeta.Pagar(Colectivo.TarifaBasica);
        Assert.That(tarjeta.Saldo, Is.EqualTo(200m - Colectivo.TarifaBasica));

        tarjeta.CargarSaldo(2000m);

        Assert.That(tarjeta.Saldo, Is.EqualTo(200m - Colectivo.TarifaBasica + 2000m));
    }

    // Se alcanza el saldo deseado mediante una carga válida y un pago previo.
    private static Tarjeta DejarTarjetaConSaldo(decimal saldoObjetivo)
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(2000m);
        tarjeta.Pagar(2000m - saldoObjetivo);
        Contexto.Db.SaveChanges();
        return tarjeta;
    }
}
