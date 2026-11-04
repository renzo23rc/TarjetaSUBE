using Microsoft.EntityFrameworkCore;
using TarjetaSUBE;

namespace TarjetaSUBETest;

// Interpretación de los límites (el enunciado no aclara los decimales):
// - Viaje 1 a 29: tarifa normal.
// - Viaje 30 a 79: 20% de descuento (se abona el 80% de la tarifa básica).
// - Viaje 80 en adelante: 25% de descuento (se abona el 75% de la tarifa básica).
// - Los viajes se cuentan por mes calendario: del primer al último día de cada mes.
public class BoletoUsoFrecuenteTests
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

        // Lunes 16/03/2026 10:00.
        _reloj = new FakeTimeProvider(new DateTime(2026, 3, 16, 10, 0, 0));
        Contexto.Reloj = _reloj;
    }

    [TearDown]
    public void TearDown()
    {
        Contexto.Reloj = TimeProvider.System;
        _db.Dispose();
    }

    [TestCase(28, 2150.0, TestName = "Viaje_29_cobra_tarifa_normal")]
    [TestCase(29, 1720.0, TestName = "Viaje_30_cobra_20_por_ciento_de_descuento")]
    [TestCase(78, 1720.0, TestName = "Viaje_79_cobra_20_por_ciento_de_descuento")]
    [TestCase(79, 1612.5, TestName = "Viaje_80_cobra_25_por_ciento_de_descuento")]
    public void TarjetaNormal_AplicaDescuentoSegunNumeroDeViaje(int viajesPrevios, double tarifaEsperada)
    {
        var tarjeta = Tarjeta.Crear(new TarjetaNormal());
        tarjeta.CargarSaldo(3000m);

        RegistrarViajes(tarjeta, viajesPrevios, new DateTime(2026, 3, 10, 10, 0, 0));

        var resultado = Colectivo.Crear("102").PagarCon(tarjeta);

        Assert.That(resultado, Is.True);

        var boleto = _db.Boletos.OrderBy(b => b.Id).Last();
        Assert.That(boleto.Tarifa, Is.EqualTo((decimal)tarifaEsperada));
        Assert.That(tarjeta.Saldo, Is.EqualTo(3000m - (decimal)tarifaEsperada));
    }

    [Test]
    public void LosViajesDeMesesAnteriores_NoCuentanParaElDescuento()
    {
        var tarjeta = Tarjeta.Crear(new TarjetaNormal());
        tarjeta.CargarSaldo(3000m);

        // 40 viajes en febrero: no deben contar para el mes de marzo.
        RegistrarViajes(tarjeta, 40, new DateTime(2026, 2, 10, 10, 0, 0));

        var resultado = Colectivo.Crear("102").PagarCon(tarjeta);

        Assert.That(resultado, Is.True);

        var boleto = _db.Boletos.OrderBy(b => b.Id).Last();
        Assert.That(boleto.Tarifa, Is.EqualTo(Colectivo.TarifaBasica));
    }

    [Test]
    public void LosViajesDelMes_SeAcumulanDesdeElPrimeroHastaElUltimoDia()
    {
        _reloj.Establecer(new DateTime(2026, 3, 31, 10, 0, 0));
        var tarjeta = Tarjeta.Crear(new TarjetaNormal());
        tarjeta.CargarSaldo(3000m);

        // 29 viajes repartidos a lo largo de marzo: el siguiente es el viaje 30.
        RegistrarViajes(tarjeta, 15, new DateTime(2026, 3, 1, 8, 0, 0));
        RegistrarViajes(tarjeta, 14, new DateTime(2026, 3, 30, 20, 0, 0));

        var resultado = Colectivo.Crear("102").PagarCon(tarjeta);

        Assert.That(resultado, Is.True);

        var boleto = _db.Boletos.OrderBy(b => b.Id).Last();
        Assert.That(boleto.Tarifa, Is.EqualTo(Colectivo.TarifaBasica * 0.80m));
    }

    [TestCase(typeof(TarjetaMedioBoleto), 1075.0, TestName = "Medio_Boleto_no_aplica_descuento")]
    [TestCase(typeof(TarjetaBoletoGratuito), 0.0, TestName = "Boleto_Gratuito_no_aplica_descuento")]
    [TestCase(typeof(TarjetaFranquiciaCompleta), 0.0, TestName = "Franquicia_Completa_no_aplica_descuento")]
    public void SoloLaTarjetaNormal_AplicaElDescuentoDeUsoFrecuente(Type tipoTarjeta, double tarifaEsperada)
    {
        var tipo = (TarjetaTipo)Activator.CreateInstance(tipoTarjeta)!;
        var tarjeta = Tarjeta.Crear(tipo);
        tarjeta.CargarSaldo(3000m);

        // 30 viajes en el mes: una tarjeta Normal ya tendría descuento, éstas no.
        RegistrarViajes(tarjeta, 30, new DateTime(2026, 3, 1, 10, 0, 0));

        var resultado = Colectivo.Crear("102").PagarCon(tarjeta);

        Assert.That(resultado, Is.True);

        var boleto = _db.Boletos.OrderBy(b => b.Id).Last();
        Assert.That(boleto.Tarifa, Is.EqualTo((decimal)tarifaEsperada));
    }

    [Test]
    public void TarjetaSinTipo_AplicaDescuentoDeUsoFrecuente()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(3000m);

        // 29 viajes en el mes: el viaje 30 tiene 20% de descuento.
        RegistrarViajes(tarjeta, 29, new DateTime(2026, 3, 10, 10, 0, 0));

        var resultado = Colectivo.Crear("102").PagarCon(tarjeta);

        Assert.That(resultado, Is.True);

        var boleto = _db.Boletos.OrderBy(b => b.Id).Last();
        Assert.That(boleto.Tarifa, Is.EqualTo(Colectivo.TarifaBasica * 0.80m));
    }

    private void RegistrarViajes(Tarjeta tarjeta, int cantidad, DateTime fecha)
    {
        for (var i = 0; i < cantidad; i++)
        {
            _db.Boletos.Add(new Boleto
            {
                TarjetaId = tarjeta.Id,
                Tarifa = Colectivo.TarifaBasica,
                TotalAbonado = Colectivo.TarifaBasica,
                SaldoRestante = 0m,
                Fecha = fecha,
                TipoTarjeta = "Normal",
                Linea = "102",
                EsTrasbordo = false,
            });
        }

        _db.SaveChanges();
    }
}
