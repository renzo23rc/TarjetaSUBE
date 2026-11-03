namespace TarjetaSUBE;

public abstract class TarjetaTipo
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;

    public abstract decimal CalcularMonto(decimal tarifaBasica);

    public virtual bool PuedeViajar(TarjetaDbContext context, int tarjetaId, DateTime ahora)
    {
        return true;
    }

    public virtual decimal CalcularTarifa(TarjetaDbContext context, Tarjeta tarjeta, DateTime ahora, decimal tarifaBasica)
    {
        return CalcularMonto(tarifaBasica);
    }
}
