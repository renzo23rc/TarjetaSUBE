namespace TarjetaSUBE;

public class TarjetaFranquiciaCompleta : TarjetaTipo
{
    public TarjetaFranquiciaCompleta()
    {
        Nombre = "Franquicia Completa";
        PorcentajeDescuento = 100m;
    }

    public override decimal CalcularMonto(decimal tarifaBasica)
    {
        return 0m;
    }
}
