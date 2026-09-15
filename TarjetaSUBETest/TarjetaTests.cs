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
        Assert.That(_db.Tarjetas.Find(tarjeta.Id), Is.Not.Null);
    }

    [Test]
    public void CargarSaldo_AceptaTodosLosMontosListados()
    {
        foreach (var monto in Tarjeta.MontosDeCarga)
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
    public void CargarSaldo_LanzaExcepcion_CuandoSuperaElSaldoMaximo()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(30000m);

        Assert.That(
            () => tarjeta.CargarSaldo(15000m),
            Throws.TypeOf<ArgumentException>());

        Assert.That(tarjeta.Saldo, Is.EqualTo(30000m));
    }

    [Test]
    public void Pagar_DescuentaLaTarifa()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(2000m);

        var resultado = tarjeta.Pagar(Colectivo.TarifaBasica);

        Assert.That(resultado, Is.True);
        Assert.That(tarjeta.Saldo, Is.EqualTo(420m));
    }

    [Test]
    public void Pagar_RetornaFalseYNoModificaSaldo_CuandoElSaldoEsInsuficiente()
    {
        var tarjeta = DejarTarjetaConSaldo(1000m);

        var resultado = tarjeta.Pagar(Colectivo.TarifaBasica);

        Assert.That(resultado, Is.False);
        Assert.That(tarjeta.Saldo, Is.EqualTo(1000m));
    }

    [Test]
    public void Pagar_LanzaExcepcion_CuandoLaTarifaNoEsPositiva()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(2000m);

        Assert.That(
            () => tarjeta.Pagar(0m),
            Throws.TypeOf<ArgumentException>());

        Assert.That(
            () => tarjeta.Pagar(-1m),
            Throws.TypeOf<ArgumentException>());

        Assert.That(tarjeta.Saldo, Is.EqualTo(2000m));
    }

    // 1000 no es un monto de carga aceptado: se llega a ese saldo
    // cargando 2000 y pagando un pasaje de 1000.
    private static Tarjeta DejarTarjetaConSaldo(decimal saldoObjetivo)
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(2000m);
        tarjeta.Pagar(2000m - saldoObjetivo);
        Contexto.Db.SaveChanges();
        return tarjeta;
    }
}
