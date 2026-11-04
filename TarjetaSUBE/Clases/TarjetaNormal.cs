namespace TarjetaSUBE;

public class TarjetaNormal : TarjetaTipo
{
    public TarjetaNormal()
    {
        Nombre = "Normal";
    }

    public override bool EsFranquicia => false;

    public override decimal CalcularMonto(decimal tarifaBasica)
    {
        return tarifaBasica;
    }

    // Boleto de uso frecuente: descuento según la cantidad de viajes del mes calendario.
    // Viaje 1 a 29: tarifa normal; viaje 30 a 79: 20% de descuento;
    // viaje 80 en adelante: 25% de descuento. Solo aplica a tarjetas Normal.
    public override decimal CalcularTarifa(TarjetaDbContext context, Tarjeta tarjeta, DateTime ahora, decimal tarifaBasica)
    {
        var viajesDelMes = context.Boletos.Count(b =>
            b.TarjetaId == tarjeta.Id && b.Fecha.Year == ahora.Year && b.Fecha.Month == ahora.Month);

        var numeroDeViaje = viajesDelMes + 1;

        if (numeroDeViaje >= 80)
        {
            return tarifaBasica * 0.75m;
        }

        if (numeroDeViaje >= 30)
        {
            return tarifaBasica * 0.80m;
        }

        return tarifaBasica;
    }
}
