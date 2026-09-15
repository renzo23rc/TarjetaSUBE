namespace TarjetaSUBE;

public class Boleto
{
    public int Id { get; set; }
    public decimal Tarifa { get; set; }
    public decimal SaldoRestante { get; set; }
    public DateTime Fecha { get; set; }
    public int TarjetaId { get; set; }
    public Tarjeta? Tarjeta { get; set; }
    public int ColectivoId { get; set; }
    public Colectivo? Colectivo { get; set; }
}
