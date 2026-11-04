namespace TarjetaSUBE;

public class TarjetaNormal : TarjetaTipo
{
    public TarjetaNormal()
    {
        Nombre = "Normal";
    }

    public override bool EsFranquicia => false;

    public override decimal CalcularMonto(decimal tarifaBasica)
    {
        return tarifaBasica;
    }
}
