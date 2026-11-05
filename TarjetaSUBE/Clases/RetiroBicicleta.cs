namespace TarjetaSUBE;

// Una multa permanece asociada al retiro que la originó hasta su pago.
public class RetiroBicicleta
{
    public int Id { get; set; }
    public long VersionConcurrencia { get; private set; }
    public int TarjetaId { get; set; }
    public Tarjeta? Tarjeta { get; set; }
    public DateTime FechaRetiro { get; set; }
    public DateTime? FechaDevolucion { get; set; }
    public decimal MultaPendiente { get; set; }
}
