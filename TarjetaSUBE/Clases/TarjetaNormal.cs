namespace TarjetaSUBE;

public class TarjetaNormal : TarjetaTipo
{
    public TarjetaNormal()
    {
        Nombre = "Normal";
    }

    public override decimal CalcularMonto(decimal tarifaBasica)
    {
        return tarifaBasica;
    }
}
