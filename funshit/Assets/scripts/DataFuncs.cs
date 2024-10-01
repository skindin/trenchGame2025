using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;

public class DataFuncs : MonoBehaviour
{
    void Update()
    {
        
    }

    public byte[] StringToBinary (string value)
    {
        return Encoding.UTF8.GetBytes (value);
    }

    public byte[] IntToBinary (int value)
    {
        return BitConverter.GetBytes (value); // nvm fuck this
    }
}
