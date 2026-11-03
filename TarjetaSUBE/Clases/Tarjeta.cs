namespace TarjetaSUBE;

public class Tarjeta
{
    public int Id { get; set; }
    public decimal Saldo { get; private set; }
    public int? TarjetaTipoId { get; set; }
    public TarjetaTipo? TarjetaTipo { get; set; }
    public DateTime? FechaPrimerBoleto { get; private set; }
    public string? LineaUltimoViaje { get; private set; }

    public const decimal SaldoMaximo = 40000m;

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

        if (Saldo + monto > SaldoMaximo)
        {
            throw new ArgumentException(
                $"La carga de {monto} supera el saldo máximo permitido de {SaldoMaximo}.");
        }

        Saldo += monto;
        Contexto.Db.SaveChanges();
    }

    public bool Pagar(decimal tarifa)
    {
        if (Saldo - tarifa < SaldoMinimo)
        {
            return false;
        }

        Saldo -= tarifa;
        return true;
    }

    public decimal CalcularTarifa(decimal tarifaBasica)
    {
        TarjetaTipo ??= TarjetaTipoId is int id
            ? Contexto.Db.TarjetaTipos.Find(id)
            : null;

        return TarjetaTipo?.CalcularMonto(tarifaBasica) ?? tarifaBasica;
    }

    public bool PuedeTransferir(string linea, DateTime ahora)
    {
        if (FechaPrimerBoleto is null || LineaUltimoViaje is null)
        {
            return false;
        }

        var esLineaDistinta = linea != LineaUltimoViaje;
        var estaDentroDeLaVentana = ahora <= FechaPrimerBoleto.Value.AddHours(1);

        return esLineaDistinta && estaDentroDeLaVentana;
    }

    public void RegistrarViaje(string linea, DateTime fecha, bool esTrasbordo)
    {
        LineaUltimoViaje = linea;

        if (!esTrasbordo)
        {
            FechaPrimerBoleto = fecha;
        }
    }
}
