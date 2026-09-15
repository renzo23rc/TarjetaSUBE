namespace TarjetaSUBE;

public class Colectivo
{
    public int Id { get; set; }
    public string Linea { get; set; } = string.Empty;

    public const decimal TarifaBasica = 1580m;

    public static Colectivo Crear(string linea)
    {
        var colectivo = new Colectivo { Linea = linea };

        Contexto.Db.Colectivos.Add(colectivo);
        Contexto.Db.SaveChanges();

        return colectivo;
    }

    public bool PagarCon(Tarjeta tarjeta)
    {
        if (!tarjeta.Pagar(TarifaBasica))
        {
            return false;
        }

        var boleto = new Boleto
        {
            Tarifa = TarifaBasica,
            SaldoRestante = tarjeta.Saldo,
            Fecha = DateTime.Now,
            Tarjeta = tarjeta,
            Colectivo = this,
        };

        Contexto.Db.Boletos.Add(boleto);
        Contexto.Db.SaveChanges();

        return true;
    }
}
