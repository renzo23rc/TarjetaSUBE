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

        var ahora = DateTime.Now;

        if (tarjeta.TarjetaTipo != null && !tarjeta.TarjetaTipo.PuedeViajar(Contexto.Db, tarjeta.Id, ahora))
        {
            return false;
        }

        var tarifa = tarjeta.TarjetaTipo?.CalcularTarifa(Contexto.Db, tarjeta, ahora, TarifaBasica) ?? TarifaBasica;
        var saldoPrevio = tarjeta.Saldo;

        if (!tarjeta.Pagar(tarifa))
        {
            return false;
        }

        var totalAbonado = tarifa;
        if (saldoPrevio < 0 && tarifa > 0)
        {
            totalAbonado = tarifa + decimal.Negate(saldoPrevio);
        }

        var boleto = new Boleto
        {
            Tarifa = tarifa,
            TotalAbonado = totalAbonado,
            SaldoRestante = tarjeta.Saldo,
            Fecha = ahora,
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
