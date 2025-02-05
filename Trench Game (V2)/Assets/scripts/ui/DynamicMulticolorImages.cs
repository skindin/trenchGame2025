using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class DynamicMulticolorImages : MonoBehaviour
{
    public List<Image> images = new();

    public void SetColor (int index, Color color)
    {
        images[index].color = color;
    }
}
