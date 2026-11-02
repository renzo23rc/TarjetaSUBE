using TarjetaSUBE;

namespace TarjetaSUBETest;

public class TarjetaTipoTests
{
    [Test]
    public void CalcularMonto_MedioBoleto_DevuelveLaMitad()
    {
        TarjetaTipo tipo = new MedioBoletoEstudiantil { Nombre = "Medio Boleto Estudiantil" };

        var resultado = tipo.CalcularMonto(Colectivo.TarifaBasica);

        Assert.That(resultado, Is.EqualTo(Colectivo.TarifaBasica / 2));
    }

    [Test]
    public void CalcularMonto_BoletoGratuito_DevuelveCero()
    {
        TarjetaTipo tipo = new BoletoGratuitoEstudiantil { Nombre = "Boleto Gratuito Estudiantil" };

        var resultado = tipo.CalcularMonto(Colectivo.TarifaBasica);

        Assert.That(resultado, Is.EqualTo(0m));
    }

    [Test]
    public void CalcularMonto_FranquiciaCompleta_DevuelveCero()
    {
        TarjetaTipo tipo = new FranquiciaCompleta { Nombre = "Franquicia Completa" };

        var resultado = tipo.CalcularMonto(Colectivo.TarifaBasica);

        Assert.That(resultado, Is.EqualTo(0m));
    }
}
