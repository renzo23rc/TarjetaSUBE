using Microsoft.EntityFrameworkCore;
using TarjetaSUBE;

namespace TarjetaSUBETest;

public class BoletoTests
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
        Contexto.Reloj = new ProveedorDeFechaSistema();
        _db.Dispose();
    }

    [Test]
    public void Boleto_GeneradoPorPagarCon_TieneLosCamposCorrectos()
    {
        var fecha = new DateTime(2026, 3, 16, 10, 0, 0);
        Contexto.Reloj = new FakeTimeProvider(fecha);
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(3000m);
        var colectivo = Colectivo.Crear("102");

        Assert.That(colectivo.PagarCon(tarjeta), Is.True);

        var boleto = _db.Boletos.AsNoTracking().Include(b => b.Tarjeta).Single();

        Assert.That(boleto.Id, Is.Not.EqualTo(0));
        Assert.That(boleto.Tarifa, Is.EqualTo(Colectivo.TarifaBasica));
        Assert.That(boleto.TotalAbonado, Is.EqualTo(Colectivo.TarifaBasica));
        Assert.That(boleto.SaldoRestante, Is.EqualTo(850m));
        Assert.That(boleto.Fecha, Is.EqualTo(fecha));
        Assert.That(boleto.TipoTarjeta, Is.EqualTo("Normal"));
        Assert.That(boleto.Linea, Is.EqualTo("102"));
        Assert.That(boleto.TarjetaId, Is.EqualTo(tarjeta.Id));
        Assert.That(boleto.ColectivoId, Is.EqualTo(colectivo.Id));
    }

}
