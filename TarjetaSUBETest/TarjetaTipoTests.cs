using Microsoft.EntityFrameworkCore;
using TarjetaSUBE;

namespace TarjetaSUBETest;

public class TarjetaTipoTests
{
    [Test]
    public void TipoNuevo_SeAgregaSinCrearUnaClase()
    {
        var opciones = new DbContextOptionsBuilder<TarjetaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var db = new TarjetaDbContext(opciones);
        Contexto.Db = db;
        Contexto.Reloj = new FakeTimeProvider(new DateTime(2026, 3, 16, 10, 0, 0));

        try
        {
            var tipo = new TarjetaTipo("Tarifa social", 25m);
            var tarjeta = Tarjeta.Crear(tipo);
            tarjeta.CargarSaldo(3000m);

            var guardado = db.TarjetaTipos.AsNoTracking().Single(t => t.Id == tipo.Id);
            Assert.That(guardado.Nombre, Is.EqualTo("Tarifa social"));
            Assert.That(guardado.PorcentajeDescuento, Is.EqualTo(25m));
            Assert.That(Colectivo.CrearInterurbano("Gálvez").PagarCon(tarjeta), Is.True);
            Assert.That(db.Boletos.Single().Tarifa, Is.EqualTo(2250m));
            Assert.That(() => new TarjetaTipo("Inválida", -1m), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new TarjetaTipo("Inválida", 101m), Throws.TypeOf<ArgumentOutOfRangeException>());
        }
        finally
        {
            Contexto.Reloj = TimeProvider.System;
        }
    }

    [Test]
    public void CalcularMonto_Normal_DevuelveTarifaCompleta()
    {
        TarjetaTipo tipo = new TarjetaNormal();

        var resultado = tipo.CalcularMonto(Colectivo.TarifaBasica);

        Assert.That(resultado, Is.EqualTo(Colectivo.TarifaBasica));
    }

    [Test]
    public void CalcularMonto_MedioBoleto_DevuelveLaMitad()
    {
        TarjetaTipo tipo = new TarjetaMedioBoleto();

        var resultado = tipo.CalcularMonto(Colectivo.TarifaBasica);

        Assert.That(resultado, Is.EqualTo(Colectivo.TarifaBasica / 2));
    }

    [Test]
    public void CalcularMonto_BoletoGratuito_DevuelveCero()
    {
        TarjetaTipo tipo = new TarjetaBoletoGratuito();

        var resultado = tipo.CalcularMonto(Colectivo.TarifaBasica);

        Assert.That(resultado, Is.EqualTo(0m));
    }

    [Test]
    public void CalcularMonto_FranquiciaCompleta_DevuelveCero()
    {
        TarjetaTipo tipo = new TarjetaFranquiciaCompleta();

        var resultado = tipo.CalcularMonto(Colectivo.TarifaBasica);

        Assert.That(resultado, Is.EqualTo(0m));
    }
}
