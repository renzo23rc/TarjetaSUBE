namespace TarjetaSUBE;

public class Colectivo
{
    public int Id { get; set; }
    public string Linea { get; set; } = string.Empty;

    public const decimal TarifaBasica = 1580m;

    public static Colectivo Crear(string linea)
    {
        var colectivo = new Colectivo { Linea = linea };

        Contexto.Db.Colectivos.Add(colectivo);
        Contexto.Db.SaveChanges();

        return colectivo;
    }

    private static readonly TimeSpan HoraInicioTrasbordo = TimeSpan.FromHours(7);
    private static readonly TimeSpan HoraFinTrasbordo = TimeSpan.FromHours(22);

    public bool PagarCon(Tarjeta tarjeta)
    {
        var ahora = Contexto.Reloj.GetLocalNow().DateTime;
        var esTrasbordo = EsHorarioDeTrasbordo(ahora) && tarjeta.PuedeTransferir(Linea, ahora);
        var tarifa = esTrasbordo ? 0m : tarjeta.CalcularTarifa(TarifaBasica);

        if (!tarjeta.Pagar(tarifa))
        {
            return false;
        }

        var boleto = new Boleto
        {
            Tarifa = tarifa,
            SaldoRestante = tarjeta.Saldo,
            Fecha = ahora,
            EsTrasbordo = esTrasbordo,
            Tarjeta = tarjeta,
            Colectivo = this,
        };

        tarjeta.RegistrarViaje(Linea, ahora, esTrasbordo);

        Contexto.Db.Boletos.Add(boleto);
        Contexto.Db.SaveChanges();

        return true;
    }

    // Los trasbordos se pueden realizar de lunes a sábado, de 7:00 (inclusive) a 22:00 (exclusive).
    private static bool EsHorarioDeTrasbordo(DateTime ahora)
    {
        var esLunesASabado = ahora.DayOfWeek is >= DayOfWeek.Monday and <= DayOfWeek.Saturday;
        var hora = ahora.TimeOfDay;
        var estaDentroDelHorario = hora >= HoraInicioTrasbordo && hora < HoraFinTrasbordo;

        return esLunesASabado && estaDentroDelHorario;
    }
}
