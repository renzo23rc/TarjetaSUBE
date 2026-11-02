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
        tarjeta.TarjetaTipo ??= tarjeta.TarjetaTipoId is int id
            ? Contexto.Db.TarjetaTipos.Find(id)
            : null;

        var monto = tarjeta.TarjetaTipo?.CalcularMonto(TarifaBasica) ?? TarifaBasica;
        var saldoPrevio = tarjeta.Saldo;

        if (!tarjeta.Pagar(monto))
        {
            return false;
        }

        var totalAbonado = monto;
        if (saldoPrevio < 0 && monto > 0)
        {
            totalAbonado = monto + decimal.Negate(saldoPrevio);
        }

        var boleto = new Boleto
        {
            Tarifa = monto,
            TotalAbonado = totalAbonado,
            Saldo = tarjeta.Saldo,
            SaldoRestante = tarjeta.Saldo,
            Fecha = DateTime.Now,
            TipoTarjeta = tarjeta.TarjetaTipo?.Nombre ?? "Normal",
            Linea = Linea,
            Tarjeta = tarjeta,
            Colectivo = this,
        };

        Contexto.Db.Boletos.Add(boleto);
        Contexto.Db.SaveChanges();

        return true;
    }
}
