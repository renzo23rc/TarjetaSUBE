namespace TarjetaSUBE;

public class MedioBoletoEstudiantil : TarjetaTipo
{
    public override decimal CalcularMonto(decimal tarifaBasica)
    {
        return tarifaBasica / 2;
    }
}
