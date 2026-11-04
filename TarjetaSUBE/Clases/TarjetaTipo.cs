namespace TarjetaSUBE;

public abstract class TarjetaTipo
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;

    // Todas las tarjetas son franquicias salvo la Normal.
    public virtual bool EsFranquicia => true;

    public abstract decimal CalcularMonto(decimal tarifaBasica);

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
