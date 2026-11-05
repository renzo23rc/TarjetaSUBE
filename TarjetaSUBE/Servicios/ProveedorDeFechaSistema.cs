namespace TarjetaSUBE;

public sealed class ProveedorDeFechaSistema : IProveedorFecha
{
    public DateTime Ahora => DateTime.Now;
}
