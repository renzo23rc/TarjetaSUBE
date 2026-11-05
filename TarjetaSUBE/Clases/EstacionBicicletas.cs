namespace TarjetaSUBE;

// Estación del sistema "Mi Bici Tu Bici".
// Para retirar una bici, la tarjeta SUBE debe pagar la tarifa diaria del servicio.
// Si el pago es rechazado (saldo insuficiente), no se permite el retiro.
// Sin descuentos por franquicia: cualquier tarjeta paga la tarifa completa.
public class EstacionBicicletas
{
    public const decimal TarifaDiaria = 2418.75m;
    public const decimal MultaPorExceso = 1250m;
    public static readonly TimeSpan TiempoMaximoUso = TimeSpan.FromHours(1);

    private readonly Dictionary<int, decimal> _multasPendientes = new();

    // Multa acumulada pendiente de pago para la tarjeta (0 si no tiene).
    public decimal MultaPendiente(Tarjeta tarjeta)
    {
        ArgumentNullException.ThrowIfNull(tarjeta);
        return _multasPendientes.TryGetValue(tarjeta.Id, out var multa) ? multa : 0m;
    }

    // Costo del próximo retiro: tarifa diaria más las multas acumuladas.
    public decimal CalcularCosto(Tarjeta tarjeta)
    {
        return TarifaDiaria + MultaPendiente(tarjeta);
    }

    // Cobra el servicio con la tarjeta. Retorna true y habilita el retiro si el
    // pago fue procesado; sino retorna false y rechaza el retiro (la multa
    // pendiente, si la hay, se conserva para el próximo intento).
    public bool RetirarBici(Tarjeta tarjeta)
    {
        ArgumentNullException.ThrowIfNull(tarjeta);

        if (!tarjeta.Pagar(CalcularCosto(tarjeta)))
        {
            return false;
        }

        _multasPendientes.Remove(tarjeta.Id);
        return true;
    }

    // Registra la devolución de la bici. Si el tiempo de uso excede el máximo
    // de 1 hora, acumula una multa de $1250 para el próximo pago.
    // Retorna true cuando se aplicó multa, false cuando la devolución fue en término.
    public bool DevolverBici(Tarjeta tarjeta, TimeSpan tiempoUso)
    {
        ArgumentNullException.ThrowIfNull(tarjeta);

        if (tiempoUso <= TiempoMaximoUso)
        {
            return false;
        }

        _multasPendientes[tarjeta.Id] = MultaPendiente(tarjeta) + MultaPorExceso;
        return true;
    }
}
