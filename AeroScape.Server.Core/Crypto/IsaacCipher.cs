namespace AeroScape.Server.Core.Crypto;

/// <summary>
/// ISAAC (Indirection, Shift, Accumulate, Add, Count) pseudo-random number generator.
/// Used by the RS 508 protocol for encrypting/decrypting packet opcodes.
/// Port of the Java ISAAC implementation used in legacy RSPS servers.
/// </summary>
public sealed class IsaacCipher
{
    private const int SizeLog = 8;
    private const int Size = 1 << SizeLog; // 256
    private const int Mask = (Size - 1) << 2; // 0x3FC, ISAAC indexes 256-word state by word offset

    private int _count;
    private readonly int[] _results = new int[Size];
    private readonly int[] _mem = new int[Size];
    private int _a, _b, _c;

    public IsaacCipher() { }

    public IsaacCipher(int[] seed)
    {
        Array.Copy(seed, 0, _results, 0, Math.Min(seed.Length, _results.Length));
        Init(true);
    }

    /// <summary>
    /// Returns the next pseudo-random value.
    /// </summary>
    public int NextInt()
    {
        if (_count-- == 0)
        {
            Isaac();
            _count = Size - 1;
        }
        return _results[_count];
    }

    private void Isaac()
    {
        int i, j, x, y;
        _b += ++_c;
        for (i = 0, j = Size / 2; i < Size / 2; )
        {
            x = _mem[i];
            _a ^= _a << 13;
            _a += _mem[j++];
            _mem[i] = y = _mem[(x & Mask) >> 2] + _a + _b;
            _results[i++] = _b = _mem[((y >> SizeLog) & Mask) >> 2] + x;

            x = _mem[i];
            _a ^= (int)((uint)_a >> 6);
            _a += _mem[j++];
            _mem[i] = y = _mem[(x & Mask) >> 2] + _a + _b;
            _results[i++] = _b = _mem[((y >> SizeLog) & Mask) >> 2] + x;

            x = _mem[i];
            _a ^= _a << 2;
            _a += _mem[j++];
            _mem[i] = y = _mem[(x & Mask) >> 2] + _a + _b;
            _results[i++] = _b = _mem[((y >> SizeLog) & Mask) >> 2] + x;

            x = _mem[i];
            _a ^= (int)((uint)_a >> 16);
            _a += _mem[j++];
            _mem[i] = y = _mem[(x & Mask) >> 2] + _a + _b;
            _results[i++] = _b = _mem[((y >> SizeLog) & Mask) >> 2] + x;
        }

        for (j = 0; j < Size / 2; )
        {
            x = _mem[i];
            _a ^= _a << 13;
            _a += _mem[j++];
            _mem[i] = y = _mem[(x & Mask) >> 2] + _a + _b;
            _results[i++] = _b = _mem[((y >> SizeLog) & Mask) >> 2] + x;

            x = _mem[i];
            _a ^= (int)((uint)_a >> 6);
            _a += _mem[j++];
            _mem[i] = y = _mem[(x & Mask) >> 2] + _a + _b;
            _results[i++] = _b = _mem[((y >> SizeLog) & Mask) >> 2] + x;

            x = _mem[i];
            _a ^= _a << 2;
            _a += _mem[j++];
            _mem[i] = y = _mem[(x & Mask) >> 2] + _a + _b;
            _results[i++] = _b = _mem[((y >> SizeLog) & Mask) >> 2] + x;

            x = _mem[i];
            _a ^= (int)((uint)_a >> 16);
            _a += _mem[j++];
            _mem[i] = y = _mem[(x & Mask) >> 2] + _a + _b;
            _results[i++] = _b = _mem[((y >> SizeLog) & Mask) >> 2] + x;
        }
    }

    private void Init(bool flag)
    {
        int a, b, c, d, e, f, g, h;
        a = b = c = d = e = f = g = h = unchecked((int)0x9e3779b9); // The golden ratio

        for (int i = 0; i < 4; ++i)
        {
            a ^= b << 11;  d += a; b += c;
            b ^= (int)((uint)b >> 2);   e += b; c += d;
            c ^= c << 8;   f += c; d += e;
            d ^= (int)((uint)d >> 16);  g += d; e += f;
            e ^= e << 10;  h += e; f += g;
            f ^= (int)((uint)f >> 4);   a += f; g += h;
            g ^= g << 8;   b += g; h += a;
            h ^= (int)((uint)h >> 9);   c += h; a += b;
        }

        for (int i = 0; i < Size; i += 8)
        {
            if (flag)
            {
                a += _results[i];     b += _results[i + 1];
                c += _results[i + 2]; d += _results[i + 3];
                e += _results[i + 4]; f += _results[i + 5];
                g += _results[i + 6]; h += _results[i + 7];
            }

            a ^= b << 11;  d += a; b += c;
            b ^= (int)((uint)b >> 2);   e += b; c += d;
            c ^= c << 8;   f += c; d += e;
            d ^= (int)((uint)d >> 16);  g += d; e += f;
            e ^= e << 10;  h += e; f += g;
            f ^= (int)((uint)f >> 4);   a += f; g += h;
            g ^= g << 8;   b += g; h += a;
            h ^= (int)((uint)h >> 9);   c += h; a += b;

            _mem[i]     = a; _mem[i + 1] = b;
            _mem[i + 2] = c; _mem[i + 3] = d;
            _mem[i + 4] = e; _mem[i + 5] = f;
            _mem[i + 6] = g; _mem[i + 7] = h;
        }

        if (flag)
        {
            for (int i = 0; i < Size; i += 8)
            {
                a += _mem[i];     b += _mem[i + 1];
                c += _mem[i + 2]; d += _mem[i + 3];
                e += _mem[i + 4]; f += _mem[i + 5];
                g += _mem[i + 6]; h += _mem[i + 7];

                a ^= b << 11;  d += a; b += c;
                b ^= (int)((uint)b >> 2);   e += b; c += d;
                c ^= c << 8;   f += c; d += e;
                d ^= (int)((uint)d >> 16);  g += d; e += f;
                e ^= e << 10;  h += e; f += g;
                f ^= (int)((uint)f >> 4);   a += f; g += h;
                g ^= g << 8;   b += g; h += a;
                h ^= (int)((uint)h >> 9);   c += h; a += b;

                _mem[i]     = a; _mem[i + 1] = b;
                _mem[i + 2] = c; _mem[i + 3] = d;
                _mem[i + 4] = e; _mem[i + 5] = f;
                _mem[i + 6] = g; _mem[i + 7] = h;
            }
        }

        Isaac();
        _count = Size;
    }
}
