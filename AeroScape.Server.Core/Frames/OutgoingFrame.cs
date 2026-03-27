using System;
using System.Buffers;
using System.IO;
using System.Text;

namespace AeroScape.Server.Core.Frames;

/// <summary>
/// Low-level outgoing packet builder for RS 508 server→client frames.
/// Mirrors the Java Stream class's createFrame/writeByte/writeWord/etc methods.
/// Writes to an expandable buffer, then flushes to a NetworkStream.
/// </summary>
public sealed class FrameWriter : IDisposable
{
    private static readonly int[] BitMaskOut =
    [
        0x0, 0x1, 0x3, 0x7, 0xf, 0x1f, 0x3f, 0x7f,
        0xff, 0x1ff, 0x3ff, 0x7ff, 0xfff, 0x1fff, 0x3fff, 0x7fff,
        0xffff, 0x1ffff, 0x3ffff, 0x7ffff, 0xfffff, 0x1fffff, 0x3fffff, 0x7fffff,
        0xffffff, 0x1ffffff, 0x3ffffff, 0x7ffffff, 0xfffffff, 0x1fffffff, 0x3fffffff, 0x7fffffff
    ];

    private byte[] _buf;
    private int _offset;
    private int _bitPosition;
    private readonly int[] _frameStack = new int[10];
    private int _frameStackPtr = -1;

    public FrameWriter(int initialCapacity = 4096)
    {
        _buf = new byte[initialCapacity];
    }

    public int Length => _offset;

    public ReadOnlySpan<byte> WrittenSpan => _buf.AsSpan(0, _offset);

    public void Reset() { _offset = 0; _bitPosition = 0; _frameStackPtr = -1; }

    // ── Frame creation (mirrors Java Stream) ────────────────────────────────

    public void CreateFrame(int opcode)
    {
        WriteByte(opcode);
    }

    public void CreateFrameVarSize(int opcode)
    {
        WriteByte(opcode);
        WriteByte(0); // placeholder for size
        _frameStack[++_frameStackPtr] = _offset;
    }

    public void CreateFrameVarSizeWord(int opcode)
    {
        WriteByte(opcode);
        WriteWord(0); // placeholder for size (2 bytes)
        _frameStack[++_frameStackPtr] = _offset;
    }

    public void EndFrameVarSize()
    {
        int start = _frameStack[_frameStackPtr--];
        int size = _offset - start;
        _buf[start - 1] = (byte)size;
    }

    public void EndFrameVarSizeWord()
    {
        int start = _frameStack[_frameStackPtr--];
        int size = _offset - start;
        _buf[start - 2] = (byte)(size >> 8);
        _buf[start - 1] = (byte)size;
    }

    // ── Primitive writes ────────────────────────────────────────────────────

    public void WriteByte(int val)
    {
        EnsureCapacity(1);
        _buf[_offset++] = (byte)val;
    }

    public void WriteByteA(int val)
    {
        WriteByte(val + 128);
    }

    public void WriteByteC(int val)
    {
        WriteByte(-val);
    }

    public void WriteByteS(int val)
    {
        WriteByte(128 - val);
    }

    public void WriteWord(int val)
    {
        EnsureCapacity(2);
        _buf[_offset++] = (byte)(val >> 8);
        _buf[_offset++] = (byte)val;
    }

    public void WriteWordBigEndian(int val)
    {
        EnsureCapacity(2);
        _buf[_offset++] = (byte)val;
        _buf[_offset++] = (byte)(val >> 8);
    }

    public void WriteWordA(int val)
    {
        EnsureCapacity(2);
        _buf[_offset++] = (byte)(val >> 8);
        _buf[_offset++] = (byte)(val + 128);
    }

    public void WriteWordBigEndianA(int val)
    {
        EnsureCapacity(2);
        _buf[_offset++] = (byte)(val + 128);
        _buf[_offset++] = (byte)(val >> 8);
    }

    public void WriteRShort(int val)
    {
        // Reverse short (little-endian)
        EnsureCapacity(2);
        _buf[_offset++] = (byte)val;
        _buf[_offset++] = (byte)(val >> 8);
    }

    public void WriteDWord(int val)
    {
        EnsureCapacity(4);
        _buf[_offset++] = (byte)(val >> 24);
        _buf[_offset++] = (byte)(val >> 16);
        _buf[_offset++] = (byte)(val >> 8);
        _buf[_offset++] = (byte)val;
    }

    public void WriteDWordBigEndian(int val)
    {
        EnsureCapacity(4);
        _buf[_offset++] = (byte)val;
        _buf[_offset++] = (byte)(val >> 8);
        _buf[_offset++] = (byte)(val >> 16);
        _buf[_offset++] = (byte)(val >> 24);
    }

    /// <summary>writeDWord_v1 — middle-endian variant 1 (bytes: 2,3,0,1)</summary>
    public void WriteDWordV1(int val)
    {
        EnsureCapacity(4);
        _buf[_offset++] = (byte)(val >> 8);
        _buf[_offset++] = (byte)val;
        _buf[_offset++] = (byte)(val >> 24);
        _buf[_offset++] = (byte)(val >> 16);
    }

    /// <summary>writeDWord_v2 — middle-endian variant 2 (bytes: 1,0,3,2)</summary>
    public void WriteDWordV2(int val)
    {
        EnsureCapacity(4);
        _buf[_offset++] = (byte)(val >> 16);
        _buf[_offset++] = (byte)(val >> 24);
        _buf[_offset++] = (byte)val;
        _buf[_offset++] = (byte)(val >> 8);
    }

    public void WriteQWord(long val)
    {
        EnsureCapacity(8);
        _buf[_offset++] = (byte)(val >> 56);
        _buf[_offset++] = (byte)(val >> 48);
        _buf[_offset++] = (byte)(val >> 40);
        _buf[_offset++] = (byte)(val >> 32);
        _buf[_offset++] = (byte)(val >> 24);
        _buf[_offset++] = (byte)(val >> 16);
        _buf[_offset++] = (byte)(val >> 8);
        _buf[_offset++] = (byte)val;
    }

    public void WriteString(string s)
    {
        foreach (char c in s)
            WriteByte(c);
        WriteByte(0);
    }

    public void WriteBytes(byte[] data, int length, int startOffset)
    {
        EnsureCapacity(length);
        Buffer.BlockCopy(data, startOffset, _buf, _offset, length);
        _offset += length;
    }

    public void InitBitAccess()
    {
        _bitPosition = _offset * 8;
    }

    public void FinishBitAccess()
    {
        _offset = (_bitPosition + 7) / 8;
    }

    public void WriteBits(int numBits, int value)
    {
        int bytePos = _bitPosition >> 3;
        int bitOffset = 8 - (_bitPosition & 7);

        EnsureCapacity(((numBits + 7) / 8) + 1);
        _bitPosition += numBits;

        for (; numBits > bitOffset; bitOffset = 8)
        {
            _buf[bytePos] &= (byte)~BitMaskOut[bitOffset];
            _buf[bytePos++] |= (byte)((value >> (numBits - bitOffset)) & BitMaskOut[bitOffset]);
            numBits -= bitOffset;
        }

        if (numBits == bitOffset)
        {
            _buf[bytePos] &= (byte)~BitMaskOut[bitOffset];
            _buf[bytePos] |= (byte)(value & BitMaskOut[bitOffset]);
            return;
        }

        _buf[bytePos] &= (byte)~(BitMaskOut[numBits] << (bitOffset - numBits));
        _buf[bytePos] |= (byte)((value & BitMaskOut[numBits]) << (bitOffset - numBits));
    }

    // ── Flush to stream ─────────────────────────────────────────────────────

    public async Task FlushToAsync(Stream stream, CancellationToken ct = default)
    {
        if (_offset > 0)
        {
            await stream.WriteAsync(_buf.AsMemory(0, _offset), ct);
            await stream.FlushAsync(ct);
            _offset = 0;
        }
    }

    // ── Capacity ────────────────────────────────────────────────────────────

    private void EnsureCapacity(int additional)
    {
        if (_offset + additional > _buf.Length)
        {
            int newSize = Math.Max(_buf.Length * 2, _offset + additional + 256);
            Array.Resize(ref _buf, newSize);
        }
    }

    public void Dispose() { }
}
