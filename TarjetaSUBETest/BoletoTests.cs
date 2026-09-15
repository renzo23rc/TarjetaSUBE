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
        _db.Dispose();
    }

    [Test]
    public void Boleto_GeneradoPorPagarCon_TieneLosCamposCorrectos()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(2000m);
        var colectivo = Colectivo.Crear("102");

        colectivo.PagarCon(tarjeta);

        var boleto = _db.Boletos.Include(b => b.Tarjeta).Single();

        Assert.That(boleto.Id, Is.Not.EqualTo(0));
        Assert.That(boleto.Tarifa, Is.EqualTo(Colectivo.TarifaBasica));
        Assert.That(boleto.SaldoRestante, Is.EqualTo(420m));
        Assert.That(boleto.Fecha, Is.Not.EqualTo(default(DateTime)));
        Assert.That(boleto.TarjetaId, Is.EqualTo(tarjeta.Id));
        Assert.That(boleto.ColectivoId, Is.EqualTo(colectivo.Id));
    }

    [Test]
    public void BoletoPersistidoDirectamente_ConservaSusCampos()
    {
        var fecha = new DateTime(2026, 3, 15, 10, 30, 0);
        var boleto = new Boleto
        {
            Tarifa = Colectivo.TarifaBasica,
            SaldoRestante = 12345m,
            Fecha = fecha,
        };

        _db.Boletos.Add(boleto);
        _db.SaveChanges();

        Assert.That(boleto.Id, Is.Not.EqualTo(0));

        var recuperado = _db.Boletos.AsNoTracking().Single(b => b.Id == boleto.Id);

        Assert.That(recuperado.Tarifa, Is.EqualTo(1580m));
        Assert.That(recuperado.SaldoRestante, Is.EqualTo(12345m));
        Assert.That(recuperado.Fecha, Is.EqualTo(fecha));
    }
}
