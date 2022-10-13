using System.Globalization;
using Disqord.Bot;

namespace Rias.Common;

public static class Helpers
{
    public static int? HexToInt(string? hex, int decimals = 6)
    {
        hex = hex?.Replace("#", string.Empty);

        if (string.IsNullOrEmpty(hex))
            return null;

        if (hex.Length != decimals)
            return null;

        if (int.TryParse(hex, NumberStyles.HexNumber, null, out var result))
            return result;

        return null;
    }
    
    public static string Stringify(this IPrefix prefix)
        => char.IsLetter(prefix.ToString()![^1]) ? prefix + " " : prefix.ToString()!;
}