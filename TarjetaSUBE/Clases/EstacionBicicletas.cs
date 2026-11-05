namespace TarjetaSUBE;

// Las estaciones comparten el historial de retiros de cada tarjeta en la base de datos.
public class EstacionBicicletas
{
    public const decimal TarifaDiaria = 2418.75m;
    public const decimal MultaPorExceso = 1250m;
    public static readonly TimeSpan TiempoMaximoUso = TimeSpan.FromHours(1);

    public decimal MultaPendiente(Tarjeta tarjeta)
    {
        ArgumentNullException.ThrowIfNull(tarjeta);
        return ObtenerRetiros(tarjeta).Sum(retiro => retiro.MultaPendiente);
    }

    public decimal CalcularCosto(Tarjeta tarjeta)
    {
        ArgumentNullException.ThrowIfNull(tarjeta);
        var retiros = ObtenerRetiros(tarjeta);
        return CalcularCosto(retiros, Contexto.Reloj.Ahora);
    }

    // Un retiro exitoso cobra la tarifa diaria (si todavía no se pagó) y todas
    // las multas pendientes. Una tarjeta solo puede tener un retiro activo.
    public bool RetirarBici(Tarjeta tarjeta)
    {
        ArgumentNullException.ThrowIfNull(tarjeta);
        var retiros = ObtenerRetiros(tarjeta);
        if (retiros.Any(retiro => retiro.FechaDevolucion == null))
        {
            return false;
        }

        var ahora = Contexto.Reloj.Ahora;
        var costo = CalcularCosto(retiros, ahora);

        if (!tarjeta.Pagar(costo))
        {
            return false;
        }

        foreach (var retiro in retiros)
        {
            retiro.MultaPendiente = 0m;
        }

        Contexto.Db.RetirosBicicletas.Add(new RetiroBicicleta { TarjetaId = tarjeta.Id, FechaRetiro = ahora });
        Contexto.Db.SaveChanges();
        return true;
    }

    // La duración se calcula usando la fecha registrada, no un valor del llamador.
    public bool DevolverBici(Tarjeta tarjeta)
    {
        ArgumentNullException.ThrowIfNull(tarjeta);
        var retiro = ObtenerRetiros(tarjeta).SingleOrDefault(r => r.FechaDevolucion == null);
        if (retiro == null)
        {
            return false;
        }

        var ahora = Contexto.Reloj.Ahora;
        if (ahora < retiro.FechaRetiro)
        {
            return false;
        }

        retiro.FechaDevolucion = ahora;
        if (ahora - retiro.FechaRetiro > TiempoMaximoUso)
        {
            retiro.MultaPendiente = MultaPorExceso;
        }

        Contexto.Db.SaveChanges();
        return true;
    }

    private static List<RetiroBicicleta> ObtenerRetiros(Tarjeta tarjeta)
    {
        return Contexto.Db.RetirosBicicletas
            .Where(retiro => retiro.TarjetaId == tarjeta.Id)
            .ToList();
    }

    private static decimal CalcularCosto(List<RetiroBicicleta> retiros, DateTime ahora)
    {
        var tarifa = retiros.Any(retiro => retiro.FechaRetiro.Date == ahora.Date) ? 0m : TarifaDiaria;
        return tarifa + retiros.Sum(retiro => retiro.MultaPendiente);
    }
}
