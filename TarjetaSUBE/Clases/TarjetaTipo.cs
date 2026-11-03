namespace TarjetaSUBE;

public abstract class TarjetaTipo
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;

    public abstract decimal CalcularMonto(decimal tarifaBasica);
}
