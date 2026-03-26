namespace AeroScape.Server.Core.Util;

/// <summary>
/// RS username ↔ long encoding. Mirrors DavidScape/util/Misc.stringToLong / longToString.
/// </summary>
public static class NameUtil
{
    private static readonly char[] ValidChars =
    {
        '_', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i',
        'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's',
        't', 'u', 'v', 'w', 'x', 'y', 'z',
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9'
    };

    public static long StringToLong(string s)
    {
        long l = 0L;
        for (int i = 0; i < s.Length && i < 12; i++)
        {
            char c = s[i];
            l *= 37L;
            if (c >= 'A' && c <= 'Z')
                l += (c + 1) - 'A';
            else if (c >= 'a' && c <= 'z')
                l += (c + 1) - 'a';
            else if (c >= '0' && c <= '9')
                l += (c + 27) - '0';
        }
        while (l % 37L == 0L && l != 0L)
            l /= 37L;
        return l;
    }

    public static string LongToString(long l)
    {
        if (l <= 0L || l >= 0x5B5B57F8A98A5DD1L)
            return "invalid_name";

        if (l % 37L == 0L)
            return "invalid_name";

        int len = 0;
        Span<char> chars = stackalloc char[12];
        while (l != 0L)
        {
            long old = l;
            l /= 37L;
            chars[11 - len++] = ValidChars[(int)(old - l * 37L)];
        }
        return new string(chars.Slice(12 - len, len));
    }

    /// <summary>Normalise a username: lowercase, replace underscores with spaces, trim.</summary>
    public static string Normalise(string name)
        => name.ToLowerInvariant().Replace('_', ' ').Trim();
}
