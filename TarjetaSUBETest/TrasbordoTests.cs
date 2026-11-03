using Microsoft.EntityFrameworkCore;
using TarjetaSUBE;

namespace TarjetaSUBETest;

public class TrasbordoTests
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

        // Lunes 16/03/2026 10:00 (dentro del horario de trasbordo).
        _reloj = new FakeTimeProvider(new DateTime(2026, 3, 16, 10, 0, 0));
        Contexto.Reloj = _reloj;
    }

    [TearDown]
    public void TearDown()
    {
        Contexto.Reloj = TimeProvider.System;
        _db.Dispose();
    }

    [Test]
    public void PrimerViaje_NoEsTrasbordo_Y_CobraTarifaNormal()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(2000m);
        var colectivo = Colectivo.Crear("102");

        var resultado = colectivo.PagarCon(tarjeta);

        Assert.That(resultado, Is.True);
        Assert.That(tarjeta.Saldo, Is.EqualTo(420m));

        var boleto = _db.Boletos.Single();
        Assert.That(boleto.EsTrasbordo, Is.False);
        Assert.That(boleto.Tarifa, Is.EqualTo(Colectivo.TarifaBasica));
    }

    [Test]
    public void SegundoViaje_EnLineaDistinta_DentroDeLaHora_EsTrasbordoGratis()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(2000m);

        var primerColectivo = Colectivo.Crear("102");
        primerColectivo.PagarCon(tarjeta);

        _reloj.Avanzar(TimeSpan.FromMinutes(30));
        var segundoColectivo = Colectivo.Crear("142");

        var resultado = segundoColectivo.PagarCon(tarjeta);

        Assert.That(resultado, Is.True);
        Assert.That(tarjeta.Saldo, Is.EqualTo(420m));

        var boleto = UltimoBoleto();
        Assert.That(boleto.EsTrasbordo, Is.True);
        Assert.That(boleto.Tarifa, Is.EqualTo(0m));
    }

    [Test]
    public void SegundoViaje_EnLaMismaLinea_NoEsTrasbordo()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(4000m);

        var primerColectivo = Colectivo.Crear("102");
        primerColectivo.PagarCon(tarjeta); // saldo 2420

        _reloj.Avanzar(TimeSpan.FromMinutes(10));
        var segundoColectivo = Colectivo.Crear("102"); // misma línea

        var resultado = segundoColectivo.PagarCon(tarjeta);

        Assert.That(resultado, Is.True);
        Assert.That(tarjeta.Saldo, Is.EqualTo(2420m - Colectivo.TarifaBasica));

        var boleto = UltimoBoleto();
        Assert.That(boleto.EsTrasbordo, Is.False);
        Assert.That(boleto.Tarifa, Is.EqualTo(Colectivo.TarifaBasica));
    }

    [Test]
    public void Trasbordo_ExactamenteUnaHoraDespues_AunEsTrasbordo()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(4000m);

        var primerColectivo = Colectivo.Crear("102");
        primerColectivo.PagarCon(tarjeta); // saldo 2420

        _reloj.Avanzar(TimeSpan.FromHours(1)); // 11:00 exacto
        var segundoColectivo = Colectivo.Crear("142");

        var resultado = segundoColectivo.PagarCon(tarjeta);

        Assert.That(resultado, Is.True);
        Assert.That(tarjeta.Saldo, Is.EqualTo(2420m));

        var boleto = UltimoBoleto();
        Assert.That(boleto.EsTrasbordo, Is.True);
        Assert.That(boleto.Tarifa, Is.EqualTo(0m));
    }

    [Test]
    public void Trasbordo_DespuesDeUnaHora_NoEsTrasbordo()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(4000m);

        var primerColectivo = Colectivo.Crear("102");
        primerColectivo.PagarCon(tarjeta); // saldo 2420

        _reloj.Avanzar(TimeSpan.FromMinutes(61)); // 11:01
        var segundoColectivo = Colectivo.Crear("142");

        var resultado = segundoColectivo.PagarCon(tarjeta);

        Assert.That(resultado, Is.True);
        Assert.That(tarjeta.Saldo, Is.EqualTo(2420m - Colectivo.TarifaBasica));

        var boleto = UltimoBoleto();
        Assert.That(boleto.EsTrasbordo, Is.False);
        Assert.That(boleto.Tarifa, Is.EqualTo(Colectivo.TarifaBasica));
    }

    [Test]
    public void Trasbordo_EnDomingo_NoEsTrasbordo()
    {
        _reloj.Establecer(new DateTime(2026, 3, 15, 10, 0, 0)); // domingo

        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(4000m);

        var primerColectivo = Colectivo.Crear("102");
        primerColectivo.PagarCon(tarjeta); // saldo 2420

        _reloj.Avanzar(TimeSpan.FromMinutes(10));
        var segundoColectivo = Colectivo.Crear("142");

        var resultado = segundoColectivo.PagarCon(tarjeta);

        Assert.That(resultado, Is.True);
        Assert.That(tarjeta.Saldo, Is.EqualTo(2420m - Colectivo.TarifaBasica));

        var boleto = UltimoBoleto();
        Assert.That(boleto.EsTrasbordo, Is.False);
        Assert.That(boleto.Tarifa, Is.EqualTo(Colectivo.TarifaBasica));
    }

    [Test]
    public void Trasbordo_AntesDeLas7_NoEsTrasbordo()
    {
        _reloj.Establecer(new DateTime(2026, 3, 16, 6, 50, 0)); // lunes 6:50

        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(4000m);

        var primerColectivo = Colectivo.Crear("102");
        primerColectivo.PagarCon(tarjeta); // saldo 2420

        _reloj.Avanzar(TimeSpan.FromMinutes(5)); // 6:55
        var segundoColectivo = Colectivo.Crear("142");

        var resultado = segundoColectivo.PagarCon(tarjeta);

        Assert.That(resultado, Is.True);
        Assert.That(tarjeta.Saldo, Is.EqualTo(2420m - Colectivo.TarifaBasica));

        var boleto = UltimoBoleto();
        Assert.That(boleto.EsTrasbordo, Is.False);
        Assert.That(boleto.Tarifa, Is.EqualTo(Colectivo.TarifaBasica));
    }

    [Test]
    public void Trasbordo_DespuesDeLas22_NoEsTrasbordo()
    {
        _reloj.Establecer(new DateTime(2026, 3, 16, 21, 50, 0)); // lunes 21:50

        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(4000m);

        var primerColectivo = Colectivo.Crear("102");
        primerColectivo.PagarCon(tarjeta); // saldo 2420

        _reloj.Avanzar(TimeSpan.FromMinutes(15)); // 22:05
        var segundoColectivo = Colectivo.Crear("142");

        var resultado = segundoColectivo.PagarCon(tarjeta);

        Assert.That(resultado, Is.True);
        Assert.That(tarjeta.Saldo, Is.EqualTo(2420m - Colectivo.TarifaBasica));

        var boleto = UltimoBoleto();
        Assert.That(boleto.EsTrasbordo, Is.False);
        Assert.That(boleto.Tarifa, Is.EqualTo(Colectivo.TarifaBasica));
    }

    [Test]
    public void TransferenciasIlimitadas_DentroDeLaHora_SonTodasGratis()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(2000m);

        var primerColectivo = Colectivo.Crear("102");
        primerColectivo.PagarCon(tarjeta); // saldo 420

        _reloj.Avanzar(TimeSpan.FromMinutes(15));
        Colectivo.Crear("142").PagarCon(tarjeta);

        _reloj.Avanzar(TimeSpan.FromMinutes(15));
        Colectivo.Crear("135").PagarCon(tarjeta);

        _reloj.Avanzar(TimeSpan.FromMinutes(15));
        Colectivo.Crear("153").PagarCon(tarjeta);

        Assert.That(tarjeta.Saldo, Is.EqualTo(420m));

        var boletos = _db.Boletos.OrderBy(b => b.Id).ToList();
        Assert.That(boletos, Has.Count.EqualTo(4));
        Assert.That(boletos[0].EsTrasbordo, Is.False);
        Assert.That(boletos.Skip(1).All(b => b.EsTrasbordo), Is.True);
        Assert.That(boletos.Skip(1).All(b => b.Tarifa == 0m), Is.True);
    }

    [Test]
    public void TarjetaMedioBoleto_TambienPuedeTransferir()
    {
        var tarjeta = Tarjeta.Crear(new TarjetaMedioBoleto());
        tarjeta.CargarSaldo(2000m);

        var primerColectivo = Colectivo.Crear("102");
        primerColectivo.PagarCon(tarjeta); // medio boleto: 790, saldo 1210

        _reloj.Avanzar(TimeSpan.FromMinutes(20));
        var segundoColectivo = Colectivo.Crear("142");

        var resultado = segundoColectivo.PagarCon(tarjeta);

        Assert.That(resultado, Is.True);
        Assert.That(tarjeta.Saldo, Is.EqualTo(1210m));

        var boleto = UltimoBoleto();
        Assert.That(boleto.EsTrasbordo, Is.True);
        Assert.That(boleto.Tarifa, Is.EqualTo(0m));
    }

    [Test]
    public void TarjetaBoletoGratuito_LaTransferenciaSeMarcaEnElBoleto()
    {
        var tarjeta = Tarjeta.Crear(new TarjetaBoletoGratuito());

        var primerColectivo = Colectivo.Crear("102");
        primerColectivo.PagarCon(tarjeta); // gratis

        _reloj.Avanzar(TimeSpan.FromMinutes(10));
        var segundoColectivo = Colectivo.Crear("142");

        segundoColectivo.PagarCon(tarjeta);

        var boleto = UltimoBoleto();
        Assert.That(boleto.EsTrasbordo, Is.True);
        Assert.That(boleto.Tarifa, Is.EqualTo(0m));
    }

    private Boleto UltimoBoleto()
    {
        return _db.Boletos.OrderBy(b => b.Id).Last();
    }
}
