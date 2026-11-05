namespace TarjetaSUBE;

public class ProveedorDeFechaSistema : IProveedorFecha
{
    public DateTime Ahora => DateTime.Now;
}
