namespace TarjetaSUBE;

public class TarjetaTipo
{
    private decimal _porcentajeDescuento;

    public TarjetaTipo() { }

    public TarjetaTipo(string nombre, decimal porcentajeDescuento)
    {
        Nombre = nombre;
        PorcentajeDescuento = porcentajeDescuento;
    }

    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal PorcentajeDescuento
    {
        get => _porcentajeDescuento;
        protected set
        {
            if (value < 0m || value > 100m)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "El descuento debe estar entre 0 y 100.");
            }

            _porcentajeDescuento = value;
        }
    }

    // Todas las tarjetas son franquicias salvo la Normal.
    public virtual bool EsFranquicia => true;

    public virtual decimal CalcularMonto(decimal tarifaBasica)
    {
        return tarifaBasica * (1m - PorcentajeDescuento / 100m);
    }

    public virtual bool PuedeViajar(TarjetaDbContext context, int tarjetaId, DateTime ahora)
    {
        return !EsFranquicia || EsHorarioDeFranquicia(ahora);
    }

    // Las franquicias solo se pueden usar de lunes a viernes de 6:00 a 22:00.
    protected static bool EsHorarioDeFranquicia(DateTime ahora)
    {
        var esLunesAViernes = ahora.DayOfWeek is >= DayOfWeek.Monday and <= DayOfWeek.Friday;
        var hora = ahora.TimeOfDay;
        var estaDentroDelHorario = hora >= TimeSpan.FromHours(6) && hora < TimeSpan.FromHours(22);

        return esLunesAViernes && estaDentroDelHorario;
    }

    public virtual decimal CalcularTarifa(TarjetaDbContext context, Tarjeta tarjeta, DateTime ahora, decimal tarifaBasica)
    {
        return CalcularMonto(tarifaBasica);
    }
}
