using Microsoft.EntityFrameworkCore;
using TarjetaSUBE;

namespace TarjetaSUBETest;

public class ColectivoTests
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

        // Lunes 16/03/2026 10:00 (dentro de la franja de las franquicias).
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
    public void Crear_GuardaElColectivoConLaLineaIndicada()
    {
        var colectivo = Colectivo.Crear("102");

        Assert.That(colectivo.Id, Is.Not.EqualTo(0));
        var guardado = _db.Colectivos.AsNoTracking().SingleOrDefault(c => c.Id == colectivo.Id);
        Assert.That(guardado, Is.Not.Null);
        Assert.That(guardado!.Linea, Is.EqualTo("102"));
    }

    [Test]
    public void PagarCon_RetornaTrue_YDescuentaLaTarifaBasica_CuandoHaySaldo()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(3000m);
        var colectivo = Colectivo.Crear("102");

        var resultado = colectivo.PagarCon(tarjeta);

        Assert.That(resultado, Is.True);
        Assert.That(tarjeta.Saldo, Is.EqualTo(850m));
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
        tarjeta.CargarSaldo(3000m);
        tarjeta.TarjetaTipoId = tipo.Id;
        _db.SaveChanges();
        var colectivo = Colectivo.Crear("102");

        var resultado = colectivo.PagarCon(tarjeta);

        Assert.That(resultado, Is.True);
        Assert.That(tarjeta.Saldo, Is.EqualTo(850m));
        var boleto = _db.Boletos.Single();
        Assert.That(boleto.Tarifa, Is.EqualTo(2150m));
        Assert.That(boleto.TipoTarjeta, Is.EqualTo("Normal"));
        Assert.That(boleto.Linea, Is.EqualTo("102"));
        Assert.That(boleto.TotalAbonado, Is.EqualTo(2150m));
        Assert.That(boleto.SaldoRestante, Is.EqualTo(850m));
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
        Assert.That(tarjeta.Saldo, Is.EqualTo(3925m));
        var boleto = _db.Boletos.Single();
        Assert.That(boleto.Tarifa, Is.EqualTo(1075m));
        Assert.That(boleto.TipoTarjeta, Is.EqualTo("Medio Boleto Estudiantil"));
        Assert.That(boleto.TotalAbonado, Is.EqualTo(1075m));
        Assert.That(boleto.SaldoRestante, Is.EqualTo(3925m));
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
        Assert.That(boleto.TipoTarjeta, Is.EqualTo("Boleto Gratuito Estudiantil"));
        Assert.That(boleto.TotalAbonado, Is.EqualTo(0m));
        Assert.That(boleto.SaldoRestante, Is.EqualTo(2000m));
    }

    [Test]
    public void PagarCon_ConFranquiciaCompletaYSaldoMinimo_PuedePagarEnHorarioHabil()
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
        Assert.That(boleto.TotalAbonado, Is.EqualTo(0m));
        Assert.That(boleto.SaldoRestante, Is.EqualTo(-2000m));
    }

    [Test]
    public void PagarCon_ConSaldoNegativo_InformaTotalAbonado()
    {
        var tipo = new TarjetaMedioBoleto();
        _db.TarjetaTipos.Add(tipo);
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(2000m);
        tarjeta.Pagar(2420m);
        Assert.That(tarjeta.Saldo, Is.EqualTo(-420m));
        tarjeta.TarjetaTipo = tipo;
        tarjeta.TarjetaTipoId = tipo.Id;
        _db.SaveChanges();
        var colectivo = Colectivo.Crear("102");

        var resultado = colectivo.PagarCon(tarjeta);

        Assert.That(resultado, Is.True);
        Assert.That(tarjeta.Saldo, Is.EqualTo(-1495m));
        var boleto = _db.Boletos.Single();
        Assert.That(boleto.Tarifa, Is.EqualTo(1075m));
        Assert.That(boleto.TotalAbonado, Is.EqualTo(1495m));
    }

    [Test]
    public void PagarCon_ConSaldoNegativoYTarifaNormal_RechazaPagoQueSuperaLimite()
    {
        var tarjeta = Tarjeta.Crear(new TarjetaNormal());
        tarjeta.CargarSaldo(2000m);
        tarjeta.Pagar(2420m);
        var colectivo = Colectivo.Crear("102");

        var resultado = colectivo.PagarCon(tarjeta);

        Assert.That(resultado, Is.False);
        Assert.That(tarjeta.Saldo, Is.EqualTo(-420m));
        Assert.That(_db.Boletos, Is.Empty);
    }

    [Test]
    public void MedioBoleto_RechazaAntesDeCincoMinutosYPuedeViajarAlCumplirlos()
    {
        var tarjeta = Tarjeta.Crear(new TarjetaMedioBoleto());
        tarjeta.CargarSaldo(5000m);
        var colectivo = Colectivo.Crear("102");

        Assert.That(colectivo.PagarCon(tarjeta), Is.True);
        _reloj.Avanzar(TimeSpan.FromMinutes(5) - TimeSpan.FromSeconds(1));
        Assert.That(colectivo.PagarCon(tarjeta), Is.False);
        Assert.That(_db.Boletos.Count(), Is.EqualTo(1));

        _reloj.Avanzar(TimeSpan.FromSeconds(1));
        Assert.That(colectivo.PagarCon(tarjeta), Is.True);
        Assert.That(_db.Boletos.Count(), Is.EqualTo(2));
        Assert.That(tarjeta.Saldo, Is.EqualTo(2850m));
    }

    [Test]
    public void MedioBoleto_AplicaDosDescuentosPorDiaYLosReiniciaAlDiaSiguiente()
    {
        var tarjeta = Tarjeta.Crear(new TarjetaMedioBoleto());
        tarjeta.CargarSaldo(10000m);
        var colectivo = Colectivo.Crear("102");

        Assert.That(colectivo.PagarCon(tarjeta), Is.True);
        _reloj.Avanzar(TimeSpan.FromMinutes(10));
        Assert.That(colectivo.PagarCon(tarjeta), Is.True);
        _reloj.Avanzar(TimeSpan.FromMinutes(10));
        Assert.That(colectivo.PagarCon(tarjeta), Is.True);

        _reloj.Avanzar(TimeSpan.FromDays(1));
        Assert.That(colectivo.PagarCon(tarjeta), Is.True);

        var tarifas = _db.Boletos.OrderBy(b => b.Id).Select(b => b.Tarifa).ToArray();
        Assert.That(tarifas, Is.EqualTo(new[] { 1075m, 1075m, 2150m, 1075m }));
        Assert.That(tarjeta.Saldo, Is.EqualTo(4625m));
    }

    [Test]
    public void MedioBoleto_TrasbordoGratis_NoConsumeElCupoDeDescuentoDiario()
    {
        var tarjeta = Tarjeta.Crear(new TarjetaMedioBoleto());
        tarjeta.CargarSaldo(10000m);

        // 10:00 L102: paga medio (1075).
        Assert.That(Colectivo.Crear("102").PagarCon(tarjeta), Is.True);
        // 10:20 L142: trasbordo gratis, no consume cupo.
        _reloj.Avanzar(TimeSpan.FromMinutes(20));
        Assert.That(Colectivo.Crear("142").PagarCon(tarjeta), Is.True);
        // 10:40 L142: misma línea que el último, se cobra con descuento.
        _reloj.Avanzar(TimeSpan.FromMinutes(20));
        Assert.That(Colectivo.Crear("142").PagarCon(tarjeta), Is.True);

        var boletos = _db.Boletos.OrderBy(b => b.Id).ToArray();
        Assert.That(boletos[1].EsTrasbordo, Is.True);
        Assert.That(boletos.Select(b => b.Tarifa).ToArray(), Is.EqualTo(new[] { 1075m, 0m, 1075m }));
        Assert.That(tarjeta.Saldo, Is.EqualTo(10000m - 1075m - 1075m));
    }

    [Test]
    public void MedioBoleto_TrasbordoGratis_NoConsumeElCupoEnInterurbano()
    {
        var tarjeta = Tarjeta.Crear(new TarjetaMedioBoleto());
        tarjeta.CargarSaldo(10000m);

        // 10:00 L102: paga medio (1075).
        Assert.That(Colectivo.Crear("102").PagarCon(tarjeta), Is.True);
        // 10:20 L142: trasbordo gratis, no consume cupo.
        _reloj.Avanzar(TimeSpan.FromMinutes(20));
        Assert.That(Colectivo.Crear("142").PagarCon(tarjeta), Is.True);
        // 11:05 interurbano: fuera de la ventana de trasbordo, se cobra con descuento.
        _reloj.Avanzar(TimeSpan.FromMinutes(45));
        Assert.That(Colectivo.CrearInterurbano("Gálvez").PagarCon(tarjeta), Is.True);

        var tarifas = _db.Boletos.OrderBy(b => b.Id).Select(b => b.Tarifa).ToArray();
        Assert.That(tarifas, Is.EqualTo(new[] { 1075m, 0m, 1500m }));
    }

    [Test]
    public void BoletoGratuito_NoPermiteMasDeDosViajesGratisPorDia()
    {
        var tarjeta = Tarjeta.Crear(new TarjetaBoletoGratuito());
        tarjeta.CargarSaldo(10000m);
        var colectivo = Colectivo.Crear("102");

        for (var i = 0; i < 3; i++)
        {
            Assert.That(colectivo.PagarCon(tarjeta), Is.True);
            _reloj.Avanzar(TimeSpan.FromMinutes(10));
        }

        Assert.That(_db.Boletos.Count(b => b.Tarifa == 0m), Is.EqualTo(2));
    }

    [Test]
    public void BoletoGratuito_ViajesPosterioresAlSegundoSeCobranConPrecioCompleto()
    {
        var tarjeta = Tarjeta.Crear(new TarjetaBoletoGratuito());
        tarjeta.CargarSaldo(10000m);
        var colectivo = Colectivo.Crear("102");

        Assert.That(colectivo.PagarCon(tarjeta), Is.True);
        _reloj.Avanzar(TimeSpan.FromMinutes(10));
        Assert.That(colectivo.PagarCon(tarjeta), Is.True);
        _reloj.Avanzar(TimeSpan.FromMinutes(10));
        Assert.That(colectivo.PagarCon(tarjeta), Is.True);
        _reloj.Avanzar(TimeSpan.FromMinutes(10));
        Assert.That(colectivo.PagarCon(tarjeta), Is.True);
        var tarifas = _db.Boletos.OrderBy(b => b.Id).Select(b => b.Tarifa).ToArray();
        Assert.That(tarifas, Is.EqualTo(new[] { 0m, 0m, 2150m, 2150m }));
    }
}
