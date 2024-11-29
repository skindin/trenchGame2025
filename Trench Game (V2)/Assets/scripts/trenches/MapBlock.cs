using System;
using UnityEngine;

public class MapBlock
{
    public byte Byte1 { private set; get; }
    public byte Byte2 { private set; get; }

    // Get a 4x4 boolean 2D array from byte1 and byte2
    //public bool[,] GetArray()
    //{
    //    bool[,] result = new bool[4, 4];

    //    for (int x = 0; x < 4; x++)
    //    {
    //        for (int y = 0; y < 4; y++)
    //        {
    //            int bitIndex = x * 4 + y;
    //            if (bitIndex < 8)
    //            {
    //                result[x, y] = (Byte1 & (1 << (7 - bitIndex))) != 0;
    //            }
    //            else
    //            {
    //                result[x, y] = (Byte2 & (1 << (15 - bitIndex))) != 0;
    //            }
    //        }
    //    }

    //    return result;
    //}

    //// Set byte1 and byte2 from a 4x4 boolean 2D array
    //public void SetArray(bool[,] array)
    //{
    //    byte tempByte1 = 0;
    //    byte tempByte2 = 0;

    //    for (int i = 0; i < 4; i++)
    //    {
    //        for (int j = 0; j < 4; j++)
    //        {
    //            int bitIndex = i * 4 + j;
    //            if (array[i, j])
    //            {
    //                if (bitIndex < 8)
    //                {
    //                    tempByte1 |= (byte)(1 << (7 - bitIndex));
    //                }
    //                else
    //                {
    //                    tempByte2 |= (byte)(1 << (15 - bitIndex));
    //                }
    //            }
    //        }
    //    }

    //    Byte1 = tempByte1;
    //    Byte2 = tempByte2;
    //}

    public bool this[Vector2Int adress]
    {
        get
        {
            return this[adress.x, adress.y];
        }
        set
        {
            this[adress.x, adress.y] = value;
        }
    }

    public bool this[int x, int y]
    {
        get
        {
            // Calculate the bit index based on grid position
            int bitIndex = x * 4 + y;

            // Validate the index is within range
            if (bitIndex < 0 || bitIndex >= 16)
            {
                throw new ArgumentOutOfRangeException($"Invalid index [{x}, {y}]");
            }

            // Retrieve the appropriate bit
            if (bitIndex < 8)
            {
                return (Byte1 & (1 << (7 - bitIndex))) != 0;
            }
            else
            {
                return (Byte2 & (1 << (15 - bitIndex))) != 0;
            }
        }

        set
        {
            // Calculate the bit index based on grid position
            int bitIndex = x * 4 + y;

            // Validate the index is within range
            if (bitIndex < 0 || bitIndex >= 16)
            {
                throw new ArgumentOutOfRangeException($"Invalid index [{x}, {y}]");
            }

            // Set or clear the bit
            if (bitIndex < 8)
            {
                if (value)
                {
                    Byte1 |= (byte)(1 << (7 - bitIndex)); // Set the bit
                }
                else
                {
                    Byte1 &= (byte)~(1 << (7 - bitIndex)); // Clear the bit
                }
            }
            else
            {
                if (value)
                {
                    Byte2 |= (byte)(1 << (15 - bitIndex)); // Set the bit
                }
                else
                {
                    Byte2 &= (byte)~(1 << (15 - bitIndex)); // Clear the bit
                }
            }
        }
    }

    public static MapBlock GetFull(bool value)
    {
        return value ? new MapBlock() {Byte1 = 0xFF, Byte2 = 0xFF} : new MapBlock { Byte1 = 0x00, Byte2 = 0x00};
    }

    public bool TestFull()
    {
        return Byte1 == 0xFF && Byte2 == 0xFF; // 0xFF = 11111111 in binary
    }

    // Check if all bits are 0
    public bool TestEmtpy()
    {
        return Byte1 == 0x00 && Byte2 == 0x00; // 0x00 = 00000000 in binary
    }

    public bool TestWhole (bool value)
    {
        return value ? TestFull() : TestEmtpy();
    }
}
