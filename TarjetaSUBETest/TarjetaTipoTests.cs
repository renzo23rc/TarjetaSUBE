using TarjetaSUBE;

namespace TarjetaSUBETest;

public class TarjetaTipoTests
{
    [Test]
    public void CalcularMonto_Normal_DevuelveLaTarifaCompleta()
    {
        var tipo = new TarjetaTipo { Nombre = "Normal", PorcentajeDescuento = 0m };

        var resultado = tipo.CalcularMonto(Colectivo.TarifaBasica);

        Assert.That(resultado, Is.EqualTo(Colectivo.TarifaBasica));
    }

    [Test]
    public void CalcularMonto_MedioBoleto_DevuelveLaMitad()
    {
        var tipo = new TarjetaTipo { Nombre = "Medio boleto estudiantil", PorcentajeDescuento = 50m };

        var resultado = tipo.CalcularMonto(Colectivo.TarifaBasica);

        Assert.That(resultado, Is.EqualTo(Colectivo.TarifaBasica / 2));
    }

    [Test]
    public void CalcularMonto_FranquiciaCompleta_DevuelveCero()
    {
        var tipo = new TarjetaTipo { Nombre = "Franquicia completa", PorcentajeDescuento = 100m };

        var resultado = tipo.CalcularMonto(Colectivo.TarifaBasica);

        Assert.That(resultado, Is.EqualTo(0m));
    }
}
