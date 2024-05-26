using System;

public struct MapBlock
{
    // Static readonly fields for full and empty MapBlock
    public static MapBlock full = 0b1111111111111111;
    public static MapBlock empty = 0b0000000000000000;

    private byte byte1; //bottom half
    private byte byte2; //upper half


    /// <summary>
    /// input must be less than 4
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public bool GetPoint(int x, int y)
    {
        if (x > 3 || y > 3)
        {
            return false;
        }

        byte pointByte = (y > 1) ? byte2 : byte1;
        int bitIndex = (y > 1) ? ((y - 2) * 4) + x : y * 4 + x;

        return ((pointByte & (1 << bitIndex)) != 0);
    }

    public void SetPoint(bool value, int x, int y)
    {
        if (x > 3 || y > 3)
        {
            return;
        }

        byte pointByte = (y > 1) ? byte2 : byte1;
        int bitIndex = (y > 1) ? ((y - 2) * 4) + x : y * 4 + x;

        if (value)
        {
            SetBit(ref pointByte, bitIndex);
        }
        else
        {
            ClearBit(ref pointByte, bitIndex);
        }

        if (y > 1)
        {
            byte2 = pointByte;
        }
        else
        {
            byte1 = pointByte;
        }
    }

    private void SetBit(ref byte value, int bitIndex)
    {
        byte mask = (byte)(1 << bitIndex);
        value |= mask;
    }

    private void ClearBit(ref byte value, int bitIndex)
    {
        byte mask = (byte)(1 << bitIndex);
        value &= (byte)~mask;
    }


    // Constructor to initialize from two bytes
    public MapBlock(byte byte1, byte byte2)
    {
        this.byte1 = byte1;
        this.byte2 = byte2;
    }

    // Implicit conversion from int to MapBlock
    public static implicit operator MapBlock(int value)
    {
        if (value < 0 || value > 0xFFFF)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Value must be between 0 and 65535 (0xFFFF).");
        }
        byte byte1 = (byte)(value & 0xFF);
        byte byte2 = (byte)((value >> 8) & 0xFF);
        return new MapBlock(byte1, byte2);
    }

    public void SetEmpty()
    {
        this = empty;
    }

    public void SetFull()
    {
        this = full;
    }

    public bool IsEmpty()
    {
        return this == empty;
    }

    public bool IsFull()
    {
        return this == full;
    }

    // Override ToString for better readability
    public override string ToString()
    {
        return $"{Convert.ToString(byte2, 2).PadLeft(8, '0')}{Convert.ToString(byte1, 2).PadLeft(8, '0')}";
    }

    // Override Equals for proper comparison
    public override bool Equals(object obj)
    {
        if (obj is MapBlock other)
        {
            return this.byte1 == other.byte1 && this.byte2 == other.byte2;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(byte1, byte2);
    }

    // Equality operator
    public static bool operator ==(MapBlock a, MapBlock b)
    {
        return a.Equals(b);
    }

    // Inequality operator
    public static bool operator !=(MapBlock a, MapBlock b)
    {
        return !a.Equals(b);
    }
}