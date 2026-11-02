namespace TarjetaSUBE;

public class TipoTarjeta
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal PorcentajeDescuento { get; set; }

    public virtual decimal CalcularMonto(decimal tarifaBasica)
    {
        return tarifaBasica * (1 - PorcentajeDescuento / 100m);
    }
}
