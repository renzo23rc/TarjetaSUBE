namespace TarjetaSUBE;

public class TarjetaMedioBoleto : TarjetaTipo
{
    public TarjetaMedioBoleto()
    {
        Nombre = "Medio Boleto Estudiantil";
    }

    public override decimal CalcularMonto(decimal tarifaBasica)
    {
        return tarifaBasica / 2;
    }
}
