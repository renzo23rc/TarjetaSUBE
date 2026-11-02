namespace TarjetaSUBE;

public class BoletoGratuitoEstudiantil : TarjetaTipo
{
    public override decimal CalcularMonto(decimal tarifaBasica)
    {
        return 0m;
    }
}
