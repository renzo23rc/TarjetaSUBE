using Microsoft.EntityFrameworkCore;
using TarjetaSUBE;

namespace TarjetaSUBETest;

public class ColectivoTests
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
    public void Crear_GuardaElColectivoConLaLineaIndicada()
    {
        var colectivo = Colectivo.Crear("102");

        Assert.That(colectivo.Id, Is.Not.EqualTo(0));
        Assert.That(colectivo.Linea, Is.EqualTo("102"));
        Assert.That(_db.Colectivos.Find(colectivo.Id), Is.Not.Null);
    }

    [Test]
    public void PagarCon_RetornaTrue_YDescuentaLaTarifaBasica_CuandoHaySaldo()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(2000m);
        var colectivo = Colectivo.Crear("102");

        var resultado = colectivo.PagarCon(tarjeta);

        Assert.That(resultado, Is.True);
        Assert.That(tarjeta.Saldo, Is.EqualTo(420m));
    }

    [Test]
    public void PagarCon_GeneraUnBoletoConLaTarifaYSaldoRestante()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(2000m);
        var colectivo = Colectivo.Crear("102");

        colectivo.PagarCon(tarjeta);

        var boletos = _db.Boletos.ToList();
        Assert.That(boletos, Has.Count.EqualTo(1));
        Assert.That(boletos[0].Tarifa, Is.EqualTo(1580m));
        Assert.That(boletos[0].SaldoRestante, Is.EqualTo(420m));
    }

    [Test]
    public void PagarCon_RetornaFalse_YNoGeneraBoleto_CuandoNoHaySaldo()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(2000m);
        tarjeta.Pagar(1000m);
        _db.SaveChanges();

        var colectivo = Colectivo.Crear("102");

        var resultado = colectivo.PagarCon(tarjeta);

        Assert.That(resultado, Is.False);
        Assert.That(_db.Boletos.ToList(), Is.Empty);
        Assert.That(tarjeta.Saldo, Is.EqualTo(1000m));
    }
}
