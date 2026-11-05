using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TarjetaSUBE;

namespace TarjetaSUBETest;

public class EstacionBicicletasTests
{
    private SqliteConnection _conexion = null!;
    private DbContextOptions<TarjetaDbContext> _opciones = null!;
    private TarjetaDbContext _db = null!;
    private FakeTimeProvider _reloj = null!;
    private EstacionBicicletas _estacion = null!;
    private InterceptorDeFallo _fallo = null!;

    [SetUp]
    public void Setup()
    {
        _conexion = new SqliteConnection("Data Source=:memory:");
        _conexion.Open();
        _fallo = new InterceptorDeFallo();
        _opciones = new DbContextOptionsBuilder<TarjetaDbContext>()
            .UseSqlite(_conexion)
            .AddInterceptors(_fallo)
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
    public void Estacion_RechazaTarjetaNoRastreadaAunqueHayaOtraInstanciaDelMismoId()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(5000m);
        var copiaNoRastreada = _db.Tarjetas.AsNoTracking().Single(t => t.Id == tarjeta.Id);

        Assert.That(() => _estacion.RetirarBici(copiaNoRastreada), Throws.ArgumentException);
        Assert.That(() => _estacion.DevolverBici(copiaNoRastreada), Throws.ArgumentException);
        Assert.That(() => _estacion.CalcularCosto(copiaNoRastreada), Throws.ArgumentException);
        Assert.That(() => _estacion.MultaPendiente(copiaNoRastreada), Throws.ArgumentException);
        Assert.That(tarjeta.Saldo, Is.EqualTo(5000m));
        Assert.That(_db.RetirosBicicletas, Is.Empty);
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
    public void RetirarBici_ConSaldoDesactualizado_RechazaSinPerderCobroYPermiteReintentar()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(10000m);
        using var contextoAnterior = new TarjetaDbContext(_opciones);
        var tarjetaAnterior = contextoAnterior.Tarjetas.Single(t => t.Id == tarjeta.Id);

        Assert.That(_estacion.RetirarBici(tarjeta), Is.True);
        _reloj.Avanzar(TimeSpan.FromMinutes(61));
        Assert.That(_estacion.DevolverBici(tarjeta), Is.True);

        Contexto.Db = contextoAnterior;
        Assert.That(() => _estacion.RetirarBici(tarjetaAnterior), Throws.TypeOf<DbUpdateConcurrencyException>());
        Assert.That(tarjetaAnterior.Saldo, Is.EqualTo(10000m));

        Contexto.Db = _db;
        Assert.That(_db.Tarjetas.AsNoTracking().Single(t => t.Id == tarjeta.Id).Saldo, Is.EqualTo(7581.25m));
        Assert.That(_estacion.MultaPendiente(tarjeta), Is.EqualTo(1250m));

        contextoAnterior.Entry(tarjetaAnterior).Reload();
        Contexto.Db = contextoAnterior;
        Assert.That(_estacion.RetirarBici(tarjetaAnterior), Is.True);
        Assert.That(tarjetaAnterior.Saldo, Is.EqualTo(6331.25m));
        Assert.That(_estacion.MultaPendiente(tarjetaAnterior), Is.Zero);
        Contexto.Db = _db;
    }

    [Test]
    public void GuardarTarjeta_ConSaldoModificadoEnOtroContexto_RechazaEscrituraObsoleta()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(10000m);
        using var otroContexto = new TarjetaDbContext(_opciones);
        var otraTarjeta = otroContexto.Tarjetas.Single(t => t.Id == tarjeta.Id);

        Assert.That(tarjeta.Pagar(1000m), Is.True);
        _db.SaveChanges();
        Assert.That(otraTarjeta.Pagar(2000m), Is.True);
        Assert.That(() => otroContexto.SaveChanges(), Throws.TypeOf<DbUpdateConcurrencyException>());
        Assert.That(_db.Tarjetas.AsNoTracking().Single(t => t.Id == tarjeta.Id).Saldo, Is.EqualTo(9000m));
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

    [Test]
    public void RetirarBici_SiFallaGuardado_RestauraOperacionYConservaCambiosPrevios()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(10000m);
        _db.RetirosBicicletas.Add(new RetiroBicicleta
        {
            TarjetaId = tarjeta.Id,
            FechaRetiro = new DateTime(2026, 3, 15, 10, 0, 0),
            FechaDevolucion = new DateTime(2026, 3, 15, 11, 1, 0),
            MultaPendiente = 1250m
        });
        _db.SaveChanges();
        tarjeta.SaldoPendiente = 123m; // Cambio previo del llamador, aún no guardado.
        _fallo.FallarDespuesDe(1);

        Assert.That(() => _estacion.RetirarBici(tarjeta), Throws.TypeOf<DbUpdateException>());
        Assert.That(_fallo.ComandosAntesDelFallo, Is.EqualTo(1));
        Assert.That(tarjeta.Saldo, Is.EqualTo(10000m));
        Assert.That(tarjeta.SaldoPendiente, Is.EqualTo(123m));
        Assert.That(_estacion.MultaPendiente(tarjeta), Is.EqualTo(1250m));
        Assert.That(_db.ChangeTracker.Entries<RetiroBicicleta>().Any(e => e.State == EntityState.Added), Is.False);
        Assert.That(_db.Tarjetas.AsNoTracking().Single(t => t.Id == tarjeta.Id).Saldo, Is.EqualTo(10000m));

        _db.SaveChanges();
        Assert.That(_db.Tarjetas.AsNoTracking().Single(t => t.Id == tarjeta.Id).SaldoPendiente, Is.EqualTo(123m));
        Assert.That(_db.RetirosBicicletas.Count(), Is.EqualTo(1));
    }

    [Test]
    public void RetirarBici_ConSaldoViejoInsuficiente_NoRechazaSinDetectarCambio()
    {
        var tarjeta = Tarjeta.Crear();
        using var otroContexto = new TarjetaDbContext(_opciones);
        var tarjetaVieja = otroContexto.Tarjetas.Single(t => t.Id == tarjeta.Id);
        tarjeta.CargarSaldo(3000m);

        Contexto.Db = otroContexto;
        Assert.That(() => _estacion.RetirarBici(tarjetaVieja), Throws.TypeOf<DbUpdateConcurrencyException>());
        otroContexto.Entry(tarjetaVieja).Reload();
        Assert.That(_estacion.RetirarBici(tarjetaVieja), Is.True);
        Assert.That(tarjetaVieja.Saldo, Is.EqualTo(581.25m));
        Contexto.Db = _db;
    }

    [Test]
    public void ConsultarMulta_ConRetiroRastreadoObsoleto_LeeDevolucionActual()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(10000m);
        Assert.That(_estacion.RetirarBici(tarjeta), Is.True);
        using var otroContexto = new TarjetaDbContext(_opciones);
        var tarjetaOtra = otroContexto.Tarjetas.Single(t => t.Id == tarjeta.Id);
        Contexto.Db = otroContexto;
        Assert.That(_estacion.MultaPendiente(tarjetaOtra), Is.Zero);

        Contexto.Db = _db;
        _reloj.Avanzar(TimeSpan.FromMinutes(61));
        Assert.That(_estacion.DevolverBici(tarjeta), Is.True);
        Contexto.Db = otroContexto;
        Assert.That(_estacion.MultaPendiente(tarjetaOtra), Is.EqualTo(1250m));
        Contexto.Db = _db;
    }

    [Test]
    public void DevolverBici_SiFallaGuardado_RestauraRetiroYPermiteReintentar()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(10000m);
        Assert.That(_estacion.RetirarBici(tarjeta), Is.True);
        _reloj.Avanzar(TimeSpan.FromMinutes(61));
        _fallo.FallarDespuesDe(0);

        Assert.That(() => _estacion.DevolverBici(tarjeta), Throws.TypeOf<DbUpdateException>());
        var retiro = _db.RetirosBicicletas.Single();
        Assert.That(retiro.FechaDevolucion, Is.Null);
        Assert.That(retiro.MultaPendiente, Is.Zero);
        Assert.That(_db.Entry(retiro).State, Is.EqualTo(EntityState.Unchanged));

        Assert.That(_estacion.DevolverBici(tarjeta), Is.True);
        Assert.That(_estacion.MultaPendiente(tarjeta), Is.EqualTo(1250m));
    }

    [Test]
    public async Task GuardarTarjetaAsync_ConSaldoModificadoEnOtroContexto_RechazaEscrituraObsoleta()
    {
        var tarjeta = Tarjeta.Crear();
        tarjeta.CargarSaldo(10000m);
        using var otroContexto = new TarjetaDbContext(_opciones);
        var otraTarjeta = otroContexto.Tarjetas.Single(t => t.Id == tarjeta.Id);

        Assert.That(tarjeta.Pagar(1000m), Is.True);
        await _db.SaveChangesAsync();
        Assert.That(otraTarjeta.Pagar(2000m), Is.True);
        Assert.That(async () => await otroContexto.SaveChangesAsync(), Throws.TypeOf<DbUpdateConcurrencyException>());
        Assert.That(_db.Tarjetas.AsNoTracking().Single(t => t.Id == tarjeta.Id).Saldo, Is.EqualTo(9000m));
    }

    // Simula un fallo durante SaveChanges sin definir sentencias SQL en la prueba.
    private class InterceptorDeFallo : DbCommandInterceptor
    {
        private int? _comandosRestantes;
        public int ComandosAntesDelFallo { get; private set; }

        public void FallarDespuesDe(int comandos)
        {
            _comandosRestantes = comandos;
            ComandosAntesDelFallo = 0;
        }

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            ComprobarFallo(eventData);
            return result;
        }

        public override InterceptionResult<int> NonQueryExecuting(
            DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
        {
            ComprobarFallo(eventData);
            return result;
        }

        private void ComprobarFallo(CommandEventData eventData)
        {
            if (_comandosRestantes is null || eventData.CommandSource != CommandSource.SaveChanges)
            {
                return;
            }

            if (_comandosRestantes == 0)
            {
                _comandosRestantes = null;
                throw new DbUpdateException("Fallo simulado de guardado.");
            }

            _comandosRestantes--;
            ComandosAntesDelFallo++;
        }
    }

    private void CambiarContexto()
    {
        _db.Dispose();
        _db = new TarjetaDbContext(_opciones);
        Contexto.Db = _db;
    }
}
