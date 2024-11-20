public class MapBlock
{
    public byte Byte1 { private set; get; }
    public byte Byte2 { private set; get; }

    public bool isEmpty { private set; get; }
    public bool isFull { private set; get; }

    // Get a 4x4 boolean 2D array from byte1 and byte2
    public bool[,] GetArray()
    {
        bool[,] result = new bool[4, 4];

        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                int bitIndex = i * 4 + j;
                if (bitIndex < 8)
                {
                    result[i, j] = (Byte1 & (1 << (7 - bitIndex))) != 0;
                }
                else
                {
                    result[i, j] = (Byte2 & (1 << (15 - bitIndex))) != 0;
                }
            }
        }

        return result;
    }

    // Set byte1 and byte2 from a 4x4 boolean 2D array
    public void SetArray(bool[,] array)
    {
        byte tempByte1 = 0;
        byte tempByte2 = 0;

        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                int bitIndex = i * 4 + j;
                if (array[i, j])
                {
                    if (bitIndex < 8)
                    {
                        tempByte1 |= (byte)(1 << (7 - bitIndex));
                    }
                    else
                    {
                        tempByte2 |= (byte)(1 << (15 - bitIndex));
                    }
                }
            }
        }

        Byte1 = tempByte1;
        Byte2 = tempByte2;

        if (TestFull())
        {
            isFull = true;
            isEmpty = false;
        }
        else if (TestEmtpy())
        {
            isFull = false;
            isEmpty = true;
        }
    }

    bool TestFull()
    {
        return Byte1 == 0xFF && Byte2 == 0xFF; // 0xFF = 11111111 in binary
    }

    // Check if all bits are 0
    bool TestEmtpy()
    {
        return Byte1 == 0x00 && Byte2 == 0x00; // 0x00 = 00000000 in binary
    }

    bool TestWhole (bool value)
    {
        return value ? TestFull() : TestEmtpy();
    }
}
