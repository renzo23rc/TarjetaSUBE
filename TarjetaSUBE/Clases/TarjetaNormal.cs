namespace TarjetaSUBE;

public class TarjetaNormal : TarjetaTipo
{
    public override decimal CalcularMonto(decimal tarifaBasica)
    {
        return tarifaBasica;
    }
}
