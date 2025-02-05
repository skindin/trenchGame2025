using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class DynamicMulticolorImages : MonoBehaviour
{
    readonly public List<Image> image = new();

    public void SetColor (int index, Color color)
    {
        image[index].color = color;
    }
}
