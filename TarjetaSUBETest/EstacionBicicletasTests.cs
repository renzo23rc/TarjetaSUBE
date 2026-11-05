using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TarjetaSUBE;

namespace TarjetaSUBETest;

public class EstacionBicicletasTests
{
    private SqliteConnection _conexion = null!;
    private DbContextOptions<TarjetaDbContext> _opciones = null!;
    private TarjetaDbContext _db = null!;
    private FakeTimeProvider _reloj = null!;
    private EstacionBicicletas _estacion = null!;

    [SetUp]
    public void Setup()
    {
        _conexion = new SqliteConnection("Data Source=:memory:");
        _conexion.Open();
        _opciones = new DbContextOptionsBuilder<TarjetaDbContext>()
            .UseSqlite(_conexion)
            .Options;
        _db = new TarjetaDbContext(_opciones);
        _db.Database.EnsureCreated();
        Contexto.Db = _db;
        _reloj = new FakeTimeProvider(new DateTime(2026, 3, 16, 10, 0, 0));
        Contexto.Reloj = _reloj;
        _estacion = new EstacionBicicletas();
    }

    [TearDown]
    public void TearDown()
    {
        Contexto.Reloj = new ProveedorDeFechaSistema();
        _db.Dispose();
        _conexion.Dispose();
    }

    [TestCase("Normal")]
    [TestCase("MedioBoleto")]
    [TestCase("BoletoGratuito")]
    [TestCase("FranquiciaCompleta")]
    public void RetirarBici_CobraTarifaCompleta_SinDescuentoParaNingunTipo(string tipo)
    {
        TarjetaTipo tarjetaTipo = tipo switch
        {
            "Normal" => new TarjetaNormal(),
            "MedioBoleto" => new TarjetaMedioBoleto(),
            "BoletoGratuito" => new TarjetaBoletoGratuito(),
            _ => new TarjetaFranquiciaCompleta()
        };
        var tarjeta = Tarjeta.Crear(tarjetaTipo);
        tarjeta.CargarSaldo(3000m);

        Assert.That(_estacion.RetirarBici(tarjeta), Is.True);
        Assert.That(tarjeta.Saldo, Is.EqualTo(581.25m));
        Assert.That(_estacion.MultaPendiente(tarjeta), Is.Zero);
    }

    [Test]
    public void RetirarBici_SinSaldoSuficiente_NoDescuentaNiCreaRetiro()
    {
        var tarjeta = Tarjeta.Crear();

        Assert.That(_estacion.RetirarBici(tarjeta), Is.False);
        Assert.That(tarjeta.Saldo, Is.Zero);
        Assert.That(_estacion.DevolverBici(tarjeta), Is.False);
    }

    [Test]
    public void RetirarBici_SoloCobraUnaVezAlDia_YVuelveACobrarAlDiaSiguiente()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(10000m);

        Assert.That(_estacion.RetirarBici(tarjeta), Is.True);
        Assert.That(_estacion.DevolverBici(tarjeta), Is.True);
        Assert.That(_estacion.RetirarBici(tarjeta), Is.True);
        Assert.That(tarjeta.Saldo, Is.EqualTo(7581.25m));
        Assert.That(_estacion.DevolverBici(tarjeta), Is.True);

        _reloj.Avanzar(TimeSpan.FromDays(1));
        Assert.That(_estacion.RetirarBici(tarjeta), Is.True);
        Assert.That(tarjeta.Saldo, Is.EqualTo(5162.50m));
    }

    [TestCase(60, 0)]
    [TestCase(61, 1250)]
    public void DevolverBici_AplicaMultaSoloAlSuperarUnaHora(int minutos, int multaEsperada)
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(3000m);
        Assert.That(_estacion.RetirarBici(tarjeta), Is.True);

        _reloj.Avanzar(TimeSpan.FromMinutes(minutos));
        Assert.That(_estacion.DevolverBici(tarjeta), Is.True);
        Assert.That(_estacion.MultaPendiente(tarjeta), Is.EqualTo(multaEsperada));
    }

    [Test]
    public void RetiroYDevolucion_NoPermitenOperacionesDuplicadasONoPropias()
    {
        var titular = Tarjeta.Crear();
        titular.CargarSaldo(5000m);
        var otraTarjeta = Tarjeta.Crear();

        Assert.That(_estacion.DevolverBici(titular), Is.False);
        Assert.That(_estacion.RetirarBici(titular), Is.True);
        Assert.That(_estacion.RetirarBici(titular), Is.False);
        Assert.That(titular.Saldo, Is.EqualTo(2581.25m));
        Assert.That(_estacion.DevolverBici(otraTarjeta), Is.False);
        Assert.That(_estacion.DevolverBici(titular), Is.True);
        Assert.That(_estacion.DevolverBici(titular), Is.False);
        Assert.That(_estacion.MultaPendiente(titular), Is.Zero);
    }


    [Test]
    public void RetirarBici_RechazaPagoYConservaMultaHastaPoderPagarla()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(3000m);
        Assert.That(_estacion.RetirarBici(tarjeta), Is.True);
        Assert.That(tarjeta.Pagar(2000m), Is.True);
        _db.SaveChanges();
        _reloj.Avanzar(TimeSpan.FromMinutes(61));
        Assert.That(_estacion.DevolverBici(tarjeta), Is.True);

        Assert.That(_estacion.RetirarBici(tarjeta), Is.False);
        Assert.That(tarjeta.Saldo, Is.EqualTo(-1418.75m));
        Assert.That(_estacion.MultaPendiente(tarjeta), Is.EqualTo(1250m));
        Assert.That(_estacion.DevolverBici(tarjeta), Is.False);

        tarjeta.CargarSaldo(3000m);
        Assert.That(_estacion.RetirarBici(tarjeta), Is.True);
        Assert.That(tarjeta.Saldo, Is.EqualTo(331.25m));
        Assert.That(_estacion.MultaPendiente(tarjeta), Is.Zero);
    }

    [Test]
    public void RetirarBici_CobraLasMultasDeUsosTardiosSucesivos()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(10000m);
        Assert.That(_estacion.RetirarBici(tarjeta), Is.True);

        _reloj.Avanzar(TimeSpan.FromMinutes(61));
        Assert.That(_estacion.DevolverBici(tarjeta), Is.True);
        Assert.That(_estacion.RetirarBici(tarjeta), Is.True);
        Assert.That(tarjeta.Saldo, Is.EqualTo(6331.25m));
        _reloj.Avanzar(TimeSpan.FromMinutes(61));
        Assert.That(_estacion.DevolverBici(tarjeta), Is.True);
        Assert.That(_estacion.RetirarBici(tarjeta), Is.True);

        Assert.That(tarjeta.Saldo, Is.EqualTo(5081.25m));
        Assert.That(_estacion.MultaPendiente(tarjeta), Is.Zero);
    }

    [Test]
    public void RetirarBici_CobraTodasLasMultasPendientesAlMismoTiempo()
    {
        var tarjeta = Tarjeta.Crear();
        var tarjetaId = tarjeta.Id;
        _db.RetirosBicicletas.AddRange(
            new RetiroBicicleta
            {
                TarjetaId = tarjeta.Id,
                FechaRetiro = new DateTime(2026, 3, 14, 10, 0, 0),
                FechaDevolucion = new DateTime(2026, 3, 14, 11, 1, 0),
                MultaPendiente = 1250m
            },
            new RetiroBicicleta
            {
                TarjetaId = tarjeta.Id,
                FechaRetiro = new DateTime(2026, 3, 15, 10, 0, 0),
                FechaDevolucion = new DateTime(2026, 3, 15, 11, 1, 0),
                MultaPendiente = 1250m
            });
        _db.SaveChanges();

        Assert.That(_estacion.MultaPendiente(tarjeta), Is.EqualTo(2500m));
        Assert.That(_estacion.CalcularCosto(tarjeta), Is.EqualTo(4918.75m));
        Assert.That(_estacion.RetirarBici(tarjeta), Is.False);

        CambiarContexto();
        tarjeta = _db.Tarjetas.Single(t => t.Id == tarjetaId);
        Assert.That(tarjeta.Saldo, Is.Zero);
        Assert.That(_estacion.MultaPendiente(tarjeta), Is.EqualTo(2500m));
        Assert.That(_estacion.DevolverBici(tarjeta), Is.False);

        tarjeta.CargarSaldo(10000m);
        Assert.That(_estacion.RetirarBici(tarjeta), Is.True);
        Assert.That(tarjeta.Saldo, Is.EqualTo(5081.25m));

        CambiarContexto();
        tarjeta = _db.Tarjetas.Single(t => t.Id == tarjetaId);
        Assert.That(_estacion.MultaPendiente(tarjeta), Is.Zero);
    }

    [Test]
    public void PagoRetiroYMulta_PersistenEnOtroContextoYOtraEstacion()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(10000m);
        var tarjetaId = tarjeta.Id;
        Assert.That(_estacion.RetirarBici(tarjeta), Is.True);

        CambiarContexto();
        tarjeta = _db.Tarjetas.Single(t => t.Id == tarjetaId);
        var otraEstacion = new EstacionBicicletas();
        Assert.That(tarjeta.Saldo, Is.EqualTo(7581.25m));
        Assert.That(otraEstacion.RetirarBici(tarjeta), Is.False);

        _reloj.Avanzar(TimeSpan.FromMinutes(61));
        Assert.That(otraEstacion.DevolverBici(tarjeta), Is.True);
        CambiarContexto();
        tarjeta = _db.Tarjetas.Single(t => t.Id == tarjetaId);
        var terceraEstacion = new EstacionBicicletas();
        Assert.That(terceraEstacion.MultaPendiente(tarjeta), Is.EqualTo(1250m));
        Assert.That(terceraEstacion.CalcularCosto(tarjeta), Is.EqualTo(1250m));
        Assert.That(terceraEstacion.RetirarBici(tarjeta), Is.True);

        CambiarContexto();
        tarjeta = _db.Tarjetas.Single(t => t.Id == tarjetaId);
        Assert.That(tarjeta.Saldo, Is.EqualTo(6331.25m));
        Assert.That(new EstacionBicicletas().MultaPendiente(tarjeta), Is.Zero);
    }



    [Test]
    public void GuardarRetiro_RechazaDosRetirosActivosParaLaMismaTarjeta()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(10000m);
        Assert.That(_estacion.RetirarBici(tarjeta), Is.True);

        using var otroContexto = new TarjetaDbContext(_opciones);
        otroContexto.RetirosBicicletas.Add(new RetiroBicicleta
        {
            TarjetaId = tarjeta.Id,
            FechaRetiro = new DateTime(2026, 3, 16, 10, 1, 0)
        });
        Assert.That(() => otroContexto.SaveChanges(), Throws.TypeOf<DbUpdateException>());
        Assert.That(_db.RetirosBicicletas.Count(r => r.TarjetaId == tarjeta.Id && r.FechaDevolucion == null), Is.EqualTo(1));
    }







    private void CambiarContexto()
    {
        _db.Dispose();
        _db = new TarjetaDbContext(_opciones);
        Contexto.Db = _db;
    }
}
