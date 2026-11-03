namespace TarjetaSUBETest;

/// <summary>
/// Proveedor de tiempo falso para testear reglas que dependen de la fecha y la hora.
/// Devuelve siempre la misma hora hasta que se la avanza o se la cambia manualmente.
/// </summary>
public sealed class FakeTimeProvider : TimeProvider
{
    private DateTime _ahora;

    public FakeTimeProvider(DateTime ahora)
    {
        _ahora = DateTime.SpecifyKind(ahora, DateTimeKind.Unspecified);
    }

    public override DateTimeOffset GetUtcNow()
    {
        return new DateTimeOffset(_ahora, TimeSpan.Zero);
    }

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    public void Avanzar(TimeSpan tiempo)
    {
        _ahora = _ahora.Add(tiempo);
    }

    public void Establecer(DateTime ahora)
    {
        _ahora = DateTime.SpecifyKind(ahora, DateTimeKind.Unspecified);
    }
}
