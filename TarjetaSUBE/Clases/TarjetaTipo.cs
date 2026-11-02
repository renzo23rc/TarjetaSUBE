namespace TarjetaSUBE;

public class TarjetaTipo
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal PorcentajeDescuento { get; set; }

    public decimal CalcularMonto(decimal tarifaBasica)
    {
        return tarifaBasica * (1 - PorcentajeDescuento / 100m);
    }
}
