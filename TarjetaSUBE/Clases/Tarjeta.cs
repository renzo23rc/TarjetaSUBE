namespace TarjetaSUBE;

public class Tarjeta
{
    public int Id { get; set; }
    public decimal Saldo { get; private set; }

    public const decimal SaldoMaximo = 40000m;

    public static readonly decimal[] MontosDeCarga =
    {
        2000m, 3000m, 4000m, 5000m, 8000m, 10000m, 15000m, 20000m, 25000m, 30000m
    };

    public static Tarjeta Crear()
    {
        var tarjeta = new Tarjeta();

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
        if (tarifa <= 0)
        {
            throw new ArgumentException("La tarifa debe ser mayor que cero.");
        }

        if (Saldo < tarifa)
        {
            return false;
        }

        Saldo -= tarifa;
        return true;
    }
}
