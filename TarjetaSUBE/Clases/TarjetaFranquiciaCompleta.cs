namespace TarjetaSUBE;

public class TarjetaFranquiciaCompleta : TarjetaTipo
{
    public TarjetaFranquiciaCompleta()
    {
        Nombre = "Franquicia Completa";
    }

    public override decimal CalcularMonto(decimal tarifaBasica)
    {
        return 0m;
    }
}
