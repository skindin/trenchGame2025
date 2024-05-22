using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrenchRenderer : MonoBehaviour
{
    public Texture2D texture;
    public SpriteRenderer spriteRenderer;
    public TrenchMap map;
    public Color fillColor = Color.black, blankColor = Color.white;
    public float width, startWidth, widthSpeed, maxWidth, mapScale;
    public int mapSize = 5;
    Vector2 lastPos;

    private void Awake()
    {
        map = new(mapSize);
        texture = new(mapSize * 4, mapSize * 4);
        spriteRenderer.sprite = Sprite.Create(texture, new(0, 0, texture.width, texture.height), Vector2.one * .5f, 1f*mapSize*4/mapScale);

        texture.filterMode = FilterMode.Point;

        //texture.mipMapBias = 0;
        //texture.anisoLevel = 1;

        texture.Apply();
    }

    private void Update()
    {
        if (Input.GetMouseButton(0))
        {
            Vector2 drawPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            drawPos =
                (Vector2.one * mapSize * 2) +
                4 * mapSize *
                drawPos /mapScale;

            if (Input.GetMouseButtonDown(0))
            {
                width = startWidth;

                map.DrawPoint(drawPos, width / 2);
            }
            else
            {
                var nextWidth = Mathf.MoveTowards(width, maxWidth, widthSpeed * Time.deltaTime);

                if (lastPos != drawPos || nextWidth != width)
                    map.DrawLine(lastPos, width/2, drawPos, nextWidth/2); //idk whether to divide or multiply map scale?

                width = nextWidth;
            }

            lastPos = drawPos;
        }

        RenderMap(map);
    }

    public void RenderMap (TrenchMap map)
    {
        for (int blockY = map.size-1; blockY >= 0; blockY--)
        {
            for (int blockX = 0; blockX < map.size; blockX++)
            {
                var block = map.blocks[blockX, blockY];

                for (int bitY = 0; bitY < 4; bitY++)
                {
                    for (int bitX = 0; bitX < 4; bitX++)
                    {
                        Color color;

                        if (block.GetPoint(bitX, bitY))
                            color = fillColor;
                        else
                            color = blankColor;

                        texture.SetPixel(blockX * 4 + bitX, blockY * 4 + bitY, color);
                    }
                }

                //DrawBlock(new(blockX, blockY));
            }
        }

        texture.Apply();
    }

    public void DrawBlock (Vector2Int pos)
    {
        var min = -(Vector2.one * mapScale/2) + ((Vector2)pos * mapScale / mapSize);
        var max = min + (Vector2.one * mapScale / mapSize);
        GeoFuncs.DrawBox(min, max, Color.black);
    }
}
