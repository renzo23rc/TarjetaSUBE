namespace TarjetaSUBE;

public class Tarjeta
{
    public int Id { get; set; }
    public decimal Saldo { get; private set; }
    public decimal SaldoPendiente { get; set; }
    public int? TarjetaTipoId { get; set; }
    public TarjetaTipo? TarjetaTipo { get; set; }

    public const decimal SaldoMaximo = 56000m;

    public const decimal SaldoMinimo = -2000m;

    public static readonly decimal[] MontosDeCarga =
    {
        2000m, 3000m, 4000m, 5000m, 8000m, 10000m, 15000m, 20000m, 25000m, 30000m
    };

    public static Tarjeta Crear(TarjetaTipo? tipo = null)
    {
        var tarjeta = new Tarjeta { TarjetaTipo = tipo };

        Contexto.Db.Tarjetas.Add(tarjeta);
        Contexto.Db.SaveChanges();

        return tarjeta;
    }

    public void CargarSaldo(decimal monto)
    {
        if (!MontosDeCarga.Contains(monto))
        {
            throw new ArgumentException(
                $"El monto de carga debe ser uno de los montos aceptados: {string.Join(", ", MontosDeCarga)}.");
        }

        SaldoPendiente += monto;
        AcreditarCarga();
        Contexto.Db.SaveChanges();
    }

    public bool Pagar(decimal tarifa)
    {
        if (tarifa < 0m || Saldo - tarifa < SaldoMinimo)
        {
            return false;
        }

        Saldo -= tarifa;
        AcreditarCarga();
        return true;
    }

    public decimal CalcularTarifa(decimal tarifaBasica)
    {
        return CalcularTarifa(tarifaBasica, Contexto.Reloj.Ahora);
    }

    internal decimal CalcularTarifa(decimal tarifaBasica, DateTime ahora)
    {
        TarjetaTipo ??= TarjetaTipoId is int id
            ? Contexto.Db.TarjetaTipos.Find(id)
            : null;

        var tipo = TarjetaTipo ?? new TarjetaNormal();
        return tipo.CalcularTarifa(Contexto.Db, this, ahora, tarifaBasica);
    }

    public decimal AcreditarCarga()
    {
        if (SaldoPendiente <= 0)
        {
            return 0m;
        }

        var espacioDisponible = SaldoMaximo - Saldo;
        if (espacioDisponible <= 0)
        {
            return 0m;
        }

        var saldoPrevio = Saldo;
        var montoAcreditado = Math.Min(SaldoPendiente, espacioDisponible);

        Saldo += montoAcreditado;
        SaldoPendiente -= montoAcreditado;

        return saldoPrevio < 0 ? Math.Min(Saldo, 0m) - saldoPrevio : 0m;
    }
}
