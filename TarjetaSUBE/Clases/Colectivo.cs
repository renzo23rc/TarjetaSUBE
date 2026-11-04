namespace TarjetaSUBE;

public class Colectivo
{
    public int Id { get; set; }
    public string Linea { get; set; } = string.Empty;
    public decimal Tarifa { get; set; } = TarifaBasica;

    public const decimal TarifaBasica = 2150m;
    public const decimal TarifaInterurbana = 3000m;

    public static Colectivo Crear(string linea)
    {
        var colectivo = new Colectivo { Linea = linea };

        Contexto.Db.Colectivos.Add(colectivo);
        Contexto.Db.SaveChanges();

        return colectivo;
    }

    public static Colectivo CrearInterurbano(string linea)
    {
        var colectivo = new Colectivo { Linea = linea, Tarifa = TarifaInterurbana };

        Contexto.Db.Colectivos.Add(colectivo);
        Contexto.Db.SaveChanges();

        return colectivo;
    }

    public bool PagarCon(Tarjeta tarjeta)
    {
        tarjeta.TarjetaTipo ??= tarjeta.TarjetaTipoId is int id
            ? Contexto.Db.TarjetaTipos.Find(id)
            : null;

        var ahora = Contexto.Reloj.GetLocalNow().DateTime;

        if (tarjeta.TarjetaTipo != null && !tarjeta.TarjetaTipo.PuedeViajar(Contexto.Db, tarjeta.Id, ahora))
        {
            return false;
        }

        var esTrasbordo = EsTrasbordo(tarjeta.Id, ahora);
        // Una tarjeta sin tipo se trata como Normal (incluye el descuento de uso frecuente).
        var tipo = tarjeta.TarjetaTipo ?? new TarjetaNormal();
        var tarifa = esTrasbordo
            ? 0m
            : tipo.CalcularTarifa(Contexto.Db, tarjeta, ahora, Tarifa);
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
            EsTrasbordo = esTrasbordo,
            Tarjeta = tarjeta,
            Colectivo = this,
        };

        Contexto.Db.Boletos.Add(boleto);
        Contexto.Db.SaveChanges();

        return true;
    }

    // Un trasbordo es gratis dentro de la hora desde el primer boleto,
    // solo entre líneas distintas, de lunes a sábado de 7:00 a 22:00.
    private bool EsTrasbordo(int tarjetaId, DateTime ahora)
    {
        if (!EsHorarioDeTrasbordo(ahora))
        {
            return false;
        }

        var ultimo = Contexto.Db.Boletos
            .Where(b => b.TarjetaId == tarjetaId)
            .OrderByDescending(b => b.Fecha)
            .ThenByDescending(b => b.Id)
            .FirstOrDefault();

        if (ultimo is null || ultimo.Linea == Linea)
        {
            return false;
        }

        var primerBoleto = Contexto.Db.Boletos
            .Where(b => b.TarjetaId == tarjetaId && !b.EsTrasbordo)
            .OrderByDescending(b => b.Fecha)
            .ThenByDescending(b => b.Id)
            .FirstOrDefault();

        return primerBoleto is not null && ahora <= primerBoleto.Fecha.AddHours(1);
    }

    private static bool EsHorarioDeTrasbordo(DateTime ahora)
    {
        var esLunesASabado = ahora.DayOfWeek is >= DayOfWeek.Monday and <= DayOfWeek.Saturday;
        var hora = ahora.TimeOfDay;
        var estaDentroDelHorario = hora >= TimeSpan.FromHours(7) && hora < TimeSpan.FromHours(22);

        return esLunesASabado && estaDentroDelHorario;
    }
}
