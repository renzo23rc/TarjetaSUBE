namespace TarjetaSUBE;

public class TarjetaMedioBoleto : TarjetaTipo
{
    public TarjetaMedioBoleto()
    {
        Nombre = "Medio Boleto Estudiantil";
        PorcentajeDescuento = 50m;
    }

    public override decimal CalcularMonto(decimal tarifaBasica)
    {
        return tarifaBasica / 2;
    }

    public override bool PuedeViajar(TarjetaDbContext context, int tarjetaId, DateTime ahora)
    {
        if (!base.PuedeViajar(context, tarjetaId, ahora))
        {
            return false;
        }

        var ultimo = context.Boletos
            .Where(b => b.TarjetaId == tarjetaId)
            .OrderByDescending(b => b.Fecha)
            .FirstOrDefault();
        if (ultimo != null && (ahora - ultimo.Fecha).TotalMinutes < 5)
        {
            return false;
        }
        return true;
    }

    public override decimal CalcularTarifa(TarjetaDbContext context, Tarjeta tarjeta, DateTime ahora, decimal tarifaBasica)
    {
        var conDescuentoHoy = context.Boletos
            .Count(b => b.TarjetaId == tarjeta.Id && b.Fecha.Date == ahora.Date && b.Tarifa < tarifaBasica);
        if (conDescuentoHoy >= 2)
        {
            return tarifaBasica;
        }
        return CalcularMonto(tarifaBasica);
    }
}
