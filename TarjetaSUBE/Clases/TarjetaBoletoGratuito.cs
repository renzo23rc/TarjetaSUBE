namespace TarjetaSUBE;

public class TarjetaBoletoGratuito : TarjetaTipo
{
    public TarjetaBoletoGratuito()
    {
        Nombre = "Boleto Gratuito Estudiantil";
    }

    public override decimal CalcularMonto(decimal tarifaBasica)
    {
        return 0m;
    }
}
