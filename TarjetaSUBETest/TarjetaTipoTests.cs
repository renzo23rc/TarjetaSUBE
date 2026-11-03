using TarjetaSUBE;

namespace TarjetaSUBETest;

public class TarjetaTipoTests
{
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
