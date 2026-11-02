using TarjetaSUBE;

namespace TarjetaSUBETest;

public class TarjetaTipoTests
{
    [Test]
    public void CalcularMonto_MedioBoleto_DevuelveLaMitad()
    {
        var tipo = new TarjetaTipo { Nombre = "Medio Boleto Estudiantil", PorcentajeDescuento = 50m };

        var resultado = tipo.CalcularMonto(Colectivo.TarifaBasica);

        Assert.That(resultado, Is.EqualTo(Colectivo.TarifaBasica / 2));
    }

    [Test]
    public void CalcularMonto_BoletoGratuito_DevuelveCero()
    {
        var tipo = new TarjetaTipo { Nombre = "Boleto Gratuito Estudiantil", PorcentajeDescuento = 100m };

        var resultado = tipo.CalcularMonto(Colectivo.TarifaBasica);

        Assert.That(resultado, Is.EqualTo(0m));
    }

    [Test]
    public void CalcularMonto_FranquiciaCompleta_DevuelveCero()
    {
        var tipo = new TarjetaTipo { Nombre = "Franquicia Completa", PorcentajeDescuento = 100m };

        var resultado = tipo.CalcularMonto(Colectivo.TarifaBasica);

        Assert.That(resultado, Is.EqualTo(0m));
    }
}
