using Microsoft.EntityFrameworkCore;

namespace TarjetaSUBE;

// Las estaciones comparten el historial de retiros de cada tarjeta en la base de datos.
// Los métodos requieren una tarjeta persistida y rastreada por Contexto.Db.
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

        VerificarSaldoActual(tarjeta);

        var entradaTarjeta = Contexto.Db.Entry(tarjeta);
        var valoresTarjeta = entradaTarjeta.CurrentValues.Clone();
        var modificadas = entradaTarjeta.Properties.Where(p => p.IsModified)
            .Select(p => p.Metadata.Name).ToHashSet();
        var valoresRetiros = retiros.Select(retiro =>
            (Retiro: retiro, Valores: Contexto.Db.Entry(retiro).CurrentValues.Clone())).ToArray();
        var nuevoRetiro = new RetiroBicicleta { TarjetaId = tarjeta.Id, FechaRetiro = ahora };

        try
        {
            if (!tarjeta.Pagar(costo))
            {
                return false;
            }

            foreach (var retiro in retiros)
            {
                retiro.MultaPendiente = 0m;
            }

            Contexto.Db.RetirosBicicletas.Add(nuevoRetiro);
            Contexto.Db.SaveChanges();
            return true;
        }
        catch
        {
            Contexto.Db.Entry(nuevoRetiro).State = EntityState.Detached;
            foreach (var (retiro, valores) in valoresRetiros)
            {
                var entrada = Contexto.Db.Entry(retiro);
                entrada.CurrentValues.SetValues(valores);
                entrada.State = EntityState.Unchanged;
            }
            entradaTarjeta.CurrentValues.SetValues(valoresTarjeta);
            foreach (var propiedad in entradaTarjeta.Properties)
            {
                var estabaModificada = modificadas.Contains(propiedad.Metadata.Name);
                if (propiedad.IsModified != estabaModificada)
                {
                    propiedad.IsModified = estabaModificada;
                }
            }
            throw;
        }
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

        var entradaRetiro = Contexto.Db.Entry(retiro);
        var valoresAnteriores = entradaRetiro.CurrentValues.Clone();
        try
        {
            retiro.FechaDevolucion = ahora;
            if (ahora - retiro.FechaRetiro > TiempoMaximoUso)
            {
                retiro.MultaPendiente = MultaPorExceso;
            }

            Contexto.Db.SaveChanges();
            return true;
        }
        catch
        {
            entradaRetiro.CurrentValues.SetValues(valoresAnteriores);
            entradaRetiro.State = EntityState.Unchanged;
            throw;
        }
    }

    private static List<RetiroBicicleta> ObtenerRetiros(Tarjeta tarjeta)
    {
        var estado = Contexto.Db.Entry(tarjeta).State;
        if (estado is not (EntityState.Unchanged or EntityState.Modified))
        {
            throw new ArgumentException("La tarjeta debe estar guardada y rastreada por el contexto actual.", nameof(tarjeta));
        }

        var entradas = Contexto.Db.ChangeTracker.Entries<RetiroBicicleta>()
            .Where(entrada => entrada.Entity.TarjetaId == tarjeta.Id).ToList();
        if (entradas.Any(entrada => entrada.State != EntityState.Unchanged))
        {
            throw new DbUpdateConcurrencyException("Hay retiros con cambios pendientes; guarde o descarte esos cambios antes de continuar.");
        }
        var rastreados = entradas.ToDictionary(entrada => entrada.Entity.Id);

        // Una consulta rastreada puede devolver una copia vieja ya presente en
        // el contexto. Consultamos valores actuales y actualizamos solo copias
        // sin cambios locales para evitar multas o devoluciones desactualizadas.
        var actuales = Contexto.Db.RetirosBicicletas.AsNoTracking()
            .Where(retiro => retiro.TarjetaId == tarjeta.Id).ToList();
        var resultado = new List<RetiroBicicleta>();
        foreach (var actual in actuales)
        {
            if (rastreados.TryGetValue(actual.Id, out var entrada))
            {
                entrada.CurrentValues.SetValues(actual);
                entrada.OriginalValues.SetValues(actual);
                resultado.Add(entrada.Entity);
            }
            else
            {
                Contexto.Db.Attach(actual);
                resultado.Add(actual);
            }
        }

        return resultado;
    }

    private static void VerificarSaldoActual(Tarjeta tarjeta)
    {
        var entrada = Contexto.Db.Entry(tarjeta);
        var valoresActuales = entrada.GetDatabaseValues();
        if (valoresActuales == null ||
            entrada.OriginalValues.GetValue<long>(nameof(Tarjeta.VersionConcurrencia)) != valoresActuales.GetValue<long>(nameof(Tarjeta.VersionConcurrencia)) ||
            entrada.OriginalValues.GetValue<decimal>(nameof(Tarjeta.Saldo)) != valoresActuales.GetValue<decimal>(nameof(Tarjeta.Saldo)) ||
            entrada.OriginalValues.GetValue<decimal>(nameof(Tarjeta.SaldoPendiente)) != valoresActuales.GetValue<decimal>(nameof(Tarjeta.SaldoPendiente)))
        {
            throw new DbUpdateConcurrencyException("El saldo de la tarjeta cambió en otro contexto. Recargue la tarjeta y reintente.");
        }
    }

    private static decimal CalcularCosto(List<RetiroBicicleta> retiros, DateTime ahora)
    {
        var tarifa = retiros.Any(retiro => retiro.FechaRetiro.Date == ahora.Date) ? 0m : TarifaDiaria;
        return tarifa + retiros.Sum(retiro => retiro.MultaPendiente);
    }
}
