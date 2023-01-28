namespace Rias.Services.Extensions;

public static class DataExtensions
{
    public const string Kilobytes = "KB";
    
    // Bytes
    public static long BytesToKilobytes(this int bytes)
    {
        return bytes / 1024;
    }
    
    public static long BytesToKilobytes(this long bytes)
    {
        return bytes / 1024;
    }
    
    // Kilobytes
    public static long KilobytesToBytes(this int kilobytes)
    {
        return kilobytes * 1024;
    }
    
    public static long KilobytesToBytes(this long kilobytes)
    {
        return kilobytes * 1024;
    }
}