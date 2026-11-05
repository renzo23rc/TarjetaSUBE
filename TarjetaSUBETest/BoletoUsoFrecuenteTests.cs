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
        Contexto.Reloj = new ProveedorDeFechaSistema();
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

        Assert.That(tarjeta.CalcularTarifa(Colectivo.TarifaBasica), Is.EqualTo((decimal)tarifaEsperada));

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

        // Ni el mes anterior ni el mismo mes del año anterior cuentan.
        RegistrarViajes(tarjeta, 40, new DateTime(2026, 2, 10, 10, 0, 0));
        RegistrarViajes(tarjeta, 40, new DateTime(2025, 3, 10, 10, 0, 0));

        var resultado = Colectivo.Crear("102").PagarCon(tarjeta);

        Assert.That(resultado, Is.True);

        var boleto = _db.Boletos.OrderBy(b => b.Id).Last();
        Assert.That(boleto.Tarifa, Is.EqualTo(Colectivo.TarifaBasica));
    }

    [Test]
    public void TarjetaSinTipo_AplicaDescuentoConViajesDeTodoElMes()
    {
        _reloj.Establecer(new DateTime(2026, 3, 31, 10, 0, 0));
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(3000m);

        // 29 viajes repartidos a lo largo de marzo: el siguiente es el viaje 30.
        RegistrarViajes(tarjeta, 15, new DateTime(2026, 3, 1, 8, 0, 0));
        RegistrarViajes(tarjeta, 14, new DateTime(2026, 3, 30, 20, 0, 0));

        var resultado = Colectivo.Crear("102").PagarCon(tarjeta);

        Assert.That(resultado, Is.True);

        var boleto = _db.Boletos.OrderBy(b => b.Id).Last();
        Assert.That(boleto.Tarifa, Is.EqualTo(Colectivo.TarifaBasica * 0.80m));
    }

    [TestCase(typeof(TarjetaMedioBoleto), 1075)]
    [TestCase(typeof(TarjetaBoletoGratuito), 0)]
    public void Franquicia_NoAplicaElDescuentoDeUsoFrecuente(Type tipoTarjeta, int tarifaConFranquicia)
    {
        var tipo = (TarjetaTipo)Activator.CreateInstance(tipoTarjeta)!;
        var tarjeta = Tarjeta.Crear(tipo);
        tarjeta.CargarSaldo(5000m);

        // 28 viajes previos: dos con franquicia y los demás a tarifa completa.
        RegistrarViajes(tarjeta, 2, new DateTime(2026, 3, 2, 10, 0, 0), tarifaConFranquicia);
        RegistrarViajes(tarjeta, 26, new DateTime(2026, 3, 2, 10, 12, 0));

        var colectivo = Colectivo.Crear("102");
        Assert.That(colectivo.PagarCon(tarjeta), Is.True);
        _reloj.Avanzar(TimeSpan.FromMinutes(10));
        Assert.That(colectivo.PagarCon(tarjeta), Is.True);
        _reloj.Avanzar(TimeSpan.FromMinutes(10));
        Assert.That(colectivo.PagarCon(tarjeta), Is.True);

        var tarifas = _db.Boletos.OrderByDescending(b => b.Id).Take(3).Select(b => b.Tarifa).ToArray();
        Assert.That(tarifas, Is.EqualTo(new[] { 2150m, (decimal)tarifaConFranquicia, (decimal)tarifaConFranquicia }));
    }

    private void RegistrarViajes(Tarjeta tarjeta, int cantidad, DateTime fecha, decimal tarifa = Colectivo.TarifaBasica)
    {
        var colectivo = Colectivo.Crear("102");

        for (var i = 0; i < cantidad; i++)
        {
            _db.Boletos.Add(new Boleto
            {
                Tarjeta = tarjeta,
                Colectivo = colectivo,
                Tarifa = tarifa,
                TotalAbonado = tarifa,
                SaldoRestante = 0m,
                Fecha = fecha.AddMinutes(i * 6),
                TipoTarjeta = tarjeta.TarjetaTipo?.Nombre ?? "Normal",
                Linea = colectivo.Linea,
                EsTrasbordo = false,
            });
        }

        _db.SaveChanges();
    }
}
