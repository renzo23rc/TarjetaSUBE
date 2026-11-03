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
    public void PagarCon_RetornaFalse_YNoGeneraBoleto_CuandoQuedariaDebajoDelMinimo()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(2000m);
        tarjeta.Pagar(3000m);
        _db.SaveChanges();

        var colectivo = Colectivo.Crear("102");

        var resultado = colectivo.PagarCon(tarjeta);

        Assert.That(resultado, Is.False);
        Assert.That(_db.Boletos.ToList(), Is.Empty);
        Assert.That(tarjeta.Saldo, Is.EqualTo(-1000m));
    }

    [Test]
    public void PagarCon_ConTarjetaNormal_CobraTarifaCompleta()
    {
        var tipo = new TarjetaNormal();
        _db.TarjetaTipos.Add(tipo);
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(2000m);
        tarjeta.TarjetaTipoId = tipo.Id;
        _db.SaveChanges();
        var colectivo = Colectivo.Crear("102");

        var resultado = colectivo.PagarCon(tarjeta);

        Assert.That(resultado, Is.True);
        Assert.That(tarjeta.Saldo, Is.EqualTo(420m));
        var boleto = _db.Boletos.Single();
        Assert.That(boleto.Tarifa, Is.EqualTo(1580m));
        Assert.That(boleto.TipoTarjeta, Is.EqualTo("Normal"));
        Assert.That(boleto.Linea, Is.EqualTo("102"));
        Assert.That(boleto.TotalAbonado, Is.EqualTo(1580m));
        Assert.That(boleto.SaldoRestante, Is.EqualTo(420m));
        Assert.That(boleto.TarjetaId, Is.EqualTo(tarjeta.Id));
    }

    [Test]
    public void PagarCon_ConMedioBoleto_CobraLaMitad()
    {
        var tipo = new TarjetaMedioBoleto();
        _db.TarjetaTipos.Add(tipo);
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(5000m);
        tarjeta.TarjetaTipo = tipo;
        tarjeta.TarjetaTipoId = tipo.Id;
        _db.SaveChanges();
        var colectivo = Colectivo.Crear("102");

        var resultado = colectivo.PagarCon(tarjeta);

        Assert.That(resultado, Is.True);
        Assert.That(tarjeta.Saldo, Is.EqualTo(4210m));
        var boleto = _db.Boletos.Single();
        Assert.That(boleto.Tarifa, Is.EqualTo(790m));
        Assert.That(boleto.TipoTarjeta, Is.EqualTo("Medio Boleto Estudiantil"));
        Assert.That(boleto.TotalAbonado, Is.EqualTo(790m));
    }

    [Test]
    public void PagarCon_ConBoletoGratuito_NoDescuentaSaldo()
    {
        var tipo = new TarjetaBoletoGratuito();
        _db.TarjetaTipos.Add(tipo);
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(2000m);
        tarjeta.TarjetaTipo = tipo;
        tarjeta.TarjetaTipoId = tipo.Id;
        _db.SaveChanges();
        var colectivo = Colectivo.Crear("102");

        var resultado = colectivo.PagarCon(tarjeta);

        Assert.That(resultado, Is.True);
        Assert.That(tarjeta.Saldo, Is.EqualTo(2000m));
        var boleto = _db.Boletos.Single();
        Assert.That(boleto.Tarifa, Is.EqualTo(0m));
    }

    [Test]
    public void PagarCon_ConFranquiciaCompleta_SiemprePuedePagar()
    {
        var tipo = new TarjetaFranquiciaCompleta();
        _db.TarjetaTipos.Add(tipo);
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(2000m);
        tarjeta.Pagar(4000m);
        tarjeta.TarjetaTipo = tipo;
        tarjeta.TarjetaTipoId = tipo.Id;
        _db.SaveChanges();
        var colectivo = Colectivo.Crear("102");

        var resultado = colectivo.PagarCon(tarjeta);

        Assert.That(resultado, Is.True);
        Assert.That(tarjeta.Saldo, Is.EqualTo(-2000m));
        var boleto = _db.Boletos.Single();
        Assert.That(boleto.Tarifa, Is.EqualTo(0m));
        Assert.That(boleto.TipoTarjeta, Is.EqualTo("Franquicia Completa"));
    }

    [Test]
    public void PagarCon_ConSaldoNegativo_InformaTotalAbonado()
    {
        var tipo = new TarjetaNormal();
        _db.TarjetaTipos.Add(tipo);
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(2000m);
        tarjeta.Pagar(2420m);
        tarjeta.TarjetaTipo = tipo;
        tarjeta.TarjetaTipoId = tipo.Id;
        _db.SaveChanges();
        var colectivo = Colectivo.Crear("102");

        var resultado = colectivo.PagarCon(tarjeta);

        Assert.That(resultado, Is.True);
        Assert.That(tarjeta.Saldo, Is.EqualTo(-2000m));
        var boleto = _db.Boletos.Single();
        Assert.That(boleto.Tarifa, Is.EqualTo(1580m));
        Assert.That(boleto.TotalAbonado, Is.EqualTo(2000m));
    }
}
