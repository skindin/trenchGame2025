using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Background : MonoBehaviour
{
    public SpriteRenderer background;
    public int checkersPerChunk = 2;
    public Color color1 = Color.white, color2 = Color.white;

    private void Start()
    {
        var totalPixels = Chunks.ChunkManager.ChunkArraySize.Value * checkersPerChunk;
        var ppu = checkersPerChunk / Chunks.ChunkManager.ChunkSize.Value;
        var texture = GenerateCheckerTexture(totalPixels, totalPixels, 1, color1, color2);
        var pivot = Vector2.one * .5f;
        background.sprite = Sprite.Create(texture,new Rect(0,0,totalPixels, totalPixels),pivot,ppu);
    }

    //private void Update()
    //{
    //    if (!target || !background)
    //        return;

    //    var adress = ChunkManager.Manager.PosToAdress(target.transform.position);
    //    transform.position = ChunkManager.Manager.AdressToPos(adress);
    //}

    Texture2D GenerateCheckerTexture(int width, int height, int checkerSize, Color color1, Color color2)
    {
        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point; // Ensure the texture is not blurred

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Determine which color to use for the current pixel
                bool isCheckerSquare = ((x / checkerSize) % 2 == 0) == ((y / checkerSize) % 2 == 0);
                Color color = isCheckerSquare ? color1 : color2;
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return texture;
    }
}
