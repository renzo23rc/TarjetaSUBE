using Microsoft.EntityFrameworkCore;
using TarjetaSUBE;

Contexto.Db.Database.EnsureCreated();

var tarjeta = Tarjeta.Crear();
tarjeta.CargarSaldo(5000m);

var colectivo = Colectivo.Crear("102");

if (colectivo.PagarCon(tarjeta))
{
    var boleto = Contexto.Db.Boletos
        .Include(b => b.Tarjeta)
        .Include(b => b.Colectivo)
        .OrderByDescending(b => b.Id)
        .First();

    Console.WriteLine($"Viaje en la línea {boleto.Colectivo!.Linea} pagado con la tarjeta #{boleto.TarjetaId}.");
    Console.WriteLine($"Tarifa: ${boleto.Tarifa}");
    Console.WriteLine($"Saldo restante: ${boleto.SaldoRestante}");
}
else
{
    Console.WriteLine("Saldo insuficiente para pagar el pasaje.");
}
