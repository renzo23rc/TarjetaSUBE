namespace TarjetaSUBETest;

using TarjetaSUBE;

/// <summary>
/// Proveedor de tiempo falso para testear reglas que dependen de la fecha y la hora.
/// Devuelve siempre la misma hora hasta que se la avanza o se la cambia manualmente.
/// Alineado a IProveedorFecha de la referencia (sin TimeProvider).
/// </summary>
public class FakeTimeProvider : IProveedorFecha
{
    private DateTime _ahora;

    public FakeTimeProvider(DateTime ahora)
    {
        _ahora = DateTime.SpecifyKind(ahora, DateTimeKind.Unspecified);
    }

    public DateTime Ahora => _ahora;

    public void Avanzar(TimeSpan tiempo)
    {
        _ahora = _ahora.Add(tiempo);
    }

    public void Establecer(DateTime ahora)
    {
        _ahora = DateTime.SpecifyKind(ahora, DateTimeKind.Unspecified);
    }
}
