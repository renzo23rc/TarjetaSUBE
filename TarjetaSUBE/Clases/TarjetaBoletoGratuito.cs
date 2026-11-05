namespace TarjetaSUBE;

public class TarjetaBoletoGratuito : TarjetaTipo
{
    public TarjetaBoletoGratuito()
    {
        Nombre = "Boleto Gratuito Estudiantil";
        PorcentajeDescuento = 100m;
    }

    public override decimal CalcularMonto(decimal tarifaBasica)
    {
        return 0m;
    }

    public override decimal CalcularTarifa(TarjetaDbContext context, Tarjeta tarjeta, DateTime ahora, decimal tarifaBasica)
    {
        var conDescuentoHoy = context.Boletos
            .Count(b => b.TarjetaId == tarjeta.Id && b.Fecha.Date == ahora.Date && b.ConDescuento);
        if (conDescuentoHoy >= 2)
        {
            return tarifaBasica;
        }
        return CalcularMonto(tarifaBasica);
    }
}
