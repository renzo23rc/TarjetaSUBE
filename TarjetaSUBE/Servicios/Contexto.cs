namespace TarjetaSUBE;

public static class Contexto
{
    public static TarjetaDbContext Db { get; set; } = new TarjetaDbContext();
}
