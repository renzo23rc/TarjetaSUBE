using Microsoft.EntityFrameworkCore;
using TarjetaSUBE;

namespace TarjetaSUBETest;

// Interpretación de los límites (el enunciado no los aclara):
// - Ventana de 1 hora: inclusiva. Un trasbordo a exactamente 1 hora del primer boleto sigue siendo trasbordo.
// - Horario: lunes a sábado, 7:00 (inclusive) y 22:00 (exclusivo). A las 22:00 ya no hay trasbordo.
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
        Contexto.Reloj = new ProveedorDeFechaSistema();
        _db.Dispose();
    }

    [Test]
    public void PrimerViajeCobraTarifaNormal_YElTrasbordoEsGratis()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(3000m);

        Colectivo.Crear("102").PagarCon(tarjeta);

        var primerBoleto = UltimoBoleto();
        Assert.That(primerBoleto.EsTrasbordo, Is.False);
        Assert.That(primerBoleto.Tarifa, Is.EqualTo(Colectivo.TarifaBasica));
        Assert.That(tarjeta.Saldo, Is.EqualTo(3000m - Colectivo.TarifaBasica));

        _reloj.Avanzar(TimeSpan.FromMinutes(30));
        var resultado = Colectivo.Crear("142").PagarCon(tarjeta);

        Assert.That(resultado, Is.True);
        Assert.That(tarjeta.Saldo, Is.EqualTo(3000m - Colectivo.TarifaBasica));

        var segundoBoleto = UltimoBoleto();
        Assert.That(segundoBoleto.EsTrasbordo, Is.True);
        Assert.That(segundoBoleto.Tarifa, Is.EqualTo(0m));
    }

    [Test]
    public void ViajeEnLaMismaLinea_NoEsTrasbordo()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(5000m);

        Colectivo.Crear("102").PagarCon(tarjeta);

        _reloj.Avanzar(TimeSpan.FromMinutes(10));
        var resultado = Colectivo.Crear("102").PagarCon(tarjeta);

        Assert.That(resultado, Is.True);
        Assert.That(tarjeta.Saldo, Is.EqualTo(5000m - Colectivo.TarifaBasica * 2));

        var boleto = UltimoBoleto();
        Assert.That(boleto.EsTrasbordo, Is.False);
        Assert.That(boleto.Tarifa, Is.EqualTo(Colectivo.TarifaBasica));
    }

    [Test]
    public void Trasbordo_ExactamenteUnaHoraDespues_SigueSiendoTrasbordo()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(5000m);

        Colectivo.Crear("102").PagarCon(tarjeta);

        _reloj.Avanzar(TimeSpan.FromHours(1));
        var resultado = Colectivo.Crear("142").PagarCon(tarjeta);

        Assert.That(resultado, Is.True);
        Assert.That(tarjeta.Saldo, Is.EqualTo(5000m - Colectivo.TarifaBasica));

        var boleto = UltimoBoleto();
        Assert.That(boleto.EsTrasbordo, Is.True);
        Assert.That(boleto.Tarifa, Is.EqualTo(0m));
    }

    [Test]
    public void Trasbordo_DespuesDeUnaHora_NoEsTrasbordo()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(5000m);

        Colectivo.Crear("102").PagarCon(tarjeta);

        _reloj.Avanzar(TimeSpan.FromMinutes(61));
        var resultado = Colectivo.Crear("142").PagarCon(tarjeta);

        Assert.That(resultado, Is.True);
        Assert.That(tarjeta.Saldo, Is.EqualTo(5000m - Colectivo.TarifaBasica * 2));

        var boleto = UltimoBoleto();
        Assert.That(boleto.EsTrasbordo, Is.False);
        Assert.That(boleto.Tarifa, Is.EqualTo(Colectivo.TarifaBasica));
    }

    [Test]
    public void TrasbordosIlimitados_DentroDeLaHora_SonGratis()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(3000m);

        Colectivo.Crear("102").PagarCon(tarjeta);

        _reloj.Avanzar(TimeSpan.FromMinutes(15));
        Colectivo.Crear("142").PagarCon(tarjeta);

        _reloj.Avanzar(TimeSpan.FromMinutes(15));
        Colectivo.Crear("135").PagarCon(tarjeta);

        _reloj.Avanzar(TimeSpan.FromMinutes(15));
        Colectivo.Crear("153").PagarCon(tarjeta);

        Assert.That(tarjeta.Saldo, Is.EqualTo(3000m - Colectivo.TarifaBasica));

        var boletos = _db.Boletos.OrderBy(b => b.Id).ToList();
        Assert.That(boletos, Has.Count.EqualTo(4));
        Assert.That(boletos[0].EsTrasbordo, Is.False);
        Assert.That(boletos.Skip(1).All(b => b.EsTrasbordo), Is.True);
        Assert.That(boletos.Skip(1).All(b => b.Tarifa == 0m), Is.True);
    }

    [TestCase(2026, 3, 15, 10, 0, TestName = "Domingo")]
    [TestCase(2026, 3, 16, 6, 50, TestName = "Antes de las 7")]
    [TestCase(2026, 3, 16, 22, 5, TestName = "Después de las 22")]
    public void Trasbordo_FueraDelHorarioPermitido_NoEsTrasbordo(int anio, int mes, int dia, int hora, int minuto)
    {
        var horaSegundoViaje = new DateTime(anio, mes, dia, hora, minuto, 0);

        _reloj.Establecer(horaSegundoViaje.AddMinutes(-10));
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(5000m);
        Colectivo.Crear("102").PagarCon(tarjeta);

        _reloj.Establecer(horaSegundoViaje);
        var resultado = Colectivo.Crear("142").PagarCon(tarjeta);

        Assert.That(resultado, Is.True);
        Assert.That(tarjeta.Saldo, Is.EqualTo(5000m - Colectivo.TarifaBasica * 2));

        var boleto = UltimoBoleto();
        Assert.That(boleto.EsTrasbordo, Is.False);
        Assert.That(boleto.Tarifa, Is.EqualTo(Colectivo.TarifaBasica));
    }

    [TestCase(typeof(TarjetaNormal))]
    [TestCase(typeof(TarjetaMedioBoleto))]
    [TestCase(typeof(TarjetaBoletoGratuito))]
    [TestCase(typeof(TarjetaFranquiciaCompleta))]
    public void TodasLasTarjetas_PuedenTransferir(Type tipoTarjeta)
    {
        var tipo = (TarjetaTipo)Activator.CreateInstance(tipoTarjeta)!;
        var tarjeta = Tarjeta.Crear(tipo);
        tarjeta.CargarSaldo(5000m);

        Colectivo.Crear("102").PagarCon(tarjeta);

        _reloj.Avanzar(TimeSpan.FromMinutes(20));
        var resultado = Colectivo.Crear("142").PagarCon(tarjeta);

        Assert.That(resultado, Is.True);

        var boleto = UltimoBoleto();
        Assert.That(boleto.EsTrasbordo, Is.True);
        Assert.That(boleto.Tarifa, Is.EqualTo(0m));
    }

    [Test]
    public void LaVentanaDeTrasbordo_SeAnclaAlPrimerBoleto()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(8000m);

        // Primer viaje a las 10:00.
        Colectivo.Crear("102").PagarCon(tarjeta);

        // Trasbordo a las 10:50 (dentro de la hora desde el primer boleto).
        _reloj.Avanzar(TimeSpan.FromMinutes(50));
        Colectivo.Crear("142").PagarCon(tarjeta);

        // Tercer viaje a las 11:10: pasaron 70 min del primer boleto (fuera de la hora),
        // aunque solo 20 min del trasbordo anterior. Debe cobrarse.
        _reloj.Avanzar(TimeSpan.FromMinutes(20));
        var resultado = Colectivo.Crear("135").PagarCon(tarjeta);

        Assert.That(resultado, Is.True);
        Assert.That(tarjeta.Saldo, Is.EqualTo(8000m - Colectivo.TarifaBasica * 2));

        var boleto = UltimoBoleto();
        Assert.That(boleto.EsTrasbordo, Is.False);
        Assert.That(boleto.Tarifa, Is.EqualTo(Colectivo.TarifaBasica));
    }

    private Boleto UltimoBoleto()
    {
        return _db.Boletos.OrderBy(b => b.Id).Last();
    }
}
