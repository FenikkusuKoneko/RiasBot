using System.Globalization;

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
}