using Microsoft.EntityFrameworkCore;
using TarjetaSUBE;

var reloj = new RelojFijo(new DateTime(2026, 10, 19, 8, 0, 0));
Contexto.Reloj = reloj;
Contexto.Db.Database.EnsureDeleted();
Contexto.Db.Database.EnsureCreated();

void Mostrar(string paso, bool ok, Tarjeta tarjeta)
{
    var boleto = Contexto.Db.Boletos
        .Include(b => b.Colectivo)
        .OrderByDescending(b => b.Id)
        .FirstOrDefault();
    Console.WriteLine($"{paso}: {(ok ? "OK" : "RECHAZADO")} | " +
        $"tarifa=${boleto?.Tarifa} total=${boleto?.TotalAbonado} " +
        $"trasbordo={boleto?.EsTrasbordo} linea={boleto?.Linea} tipo={boleto?.TipoTarjeta} | " +
        $"saldo=${tarjeta.Saldo} pendiente=${tarjeta.SaldoPendiente}");
}

var tarjeta = Tarjeta.Crear();
tarjeta.CargarSaldo(10000m);
var urbano102 = Colectivo.Crear("102");
Mostrar("1. Normal urbano", urbano102.PagarCon(tarjeta), tarjeta);

reloj.Ahora = reloj.Ahora.AddMinutes(20);
var urbano103 = Colectivo.Crear("103");
Mostrar("2. Trasbordo 102->103", urbano103.PagarCon(tarjeta), tarjeta);

reloj.Ahora = new DateTime(2026, 10, 19, 9, 10, 0);
tarjeta.TarjetaTipo = new TarjetaMedioBoleto();
Contexto.Db.SaveChanges();
Mostrar("3. Medio boleto", urbano102.PagarCon(tarjeta), tarjeta);

reloj.Ahora = reloj.Ahora.AddMinutes(10);
tarjeta.TarjetaTipo = new TarjetaFranquiciaCompleta();
Contexto.Db.SaveChanges();
Mostrar("4. Franquicia completa", urbano102.PagarCon(tarjeta), tarjeta);

reloj.Ahora = new DateTime(2026, 10, 19, 10, 30, 0);
tarjeta.TarjetaTipo = new TarjetaNormal();
Contexto.Db.SaveChanges();
var inter = Colectivo.CrearInterurbano("Galvez");
Mostrar("5. Interurbano normal", inter.PagarCon(tarjeta), tarjeta);

var estacion = new EstacionBicicletas();
bool retiro = estacion.RetirarBici(tarjeta);
Console.WriteLine($"6. Bici retira: {(retiro ? "OK" : "RECHAZADO")} | saldo=${tarjeta.Saldo}");
reloj.Ahora = reloj.Ahora.AddMinutes(40);
bool devuelve = estacion.DevolverBici(tarjeta);
Console.WriteLine($"   Bici devuelve 40min: {(devuelve ? "OK sin multa" : "FALLO")} | " +
    $"multa=${estacion.MultaPendiente(tarjeta)} saldo=${tarjeta.Saldo}");

class RelojFijo(DateTime inicial) : IProveedorFecha
{
    public DateTime Ahora { get; set; } = inicial;
}
