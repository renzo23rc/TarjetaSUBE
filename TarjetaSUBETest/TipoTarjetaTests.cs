using TarjetaSUBE;

namespace TarjetaSUBETest;

public class TipoTarjetaTests
{
    [Test]
    public void CalcularMonto_Normal_DevuelveTarifaCompleta()
    {
        var tipo = new TipoTarjeta { Nombre = "Normal", PorcentajeDescuento = 0m };

        var resultado = tipo.CalcularMonto(Colectivo.TarifaBasica);

        Assert.That(resultado, Is.EqualTo(Colectivo.TarifaBasica));
    }

    [Test]
    public void CalcularMonto_MedioBoleto_DevuelveLaMitad()
    {
        var tipo = new TipoTarjeta { Nombre = "Medio Boleto Estudiantil", PorcentajeDescuento = 50m };

        var resultado = tipo.CalcularMonto(Colectivo.TarifaBasica);

        Assert.That(resultado, Is.EqualTo(Colectivo.TarifaBasica / 2));
    }

    [Test]
    public void CalcularMonto_FranquiciaCompleta_DevuelveCero()
    {
        var tipo = new TipoTarjeta { Nombre = "Franquicia Completa", PorcentajeDescuento = 100m };

        var resultado = tipo.CalcularMonto(Colectivo.TarifaBasica);

        Assert.That(resultado, Is.EqualTo(0m));
    }
}
