using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrenchMap : MonoBehaviour
{
    public Sprite sprite;
    public SpriteRenderer spriteRenderer;
    public MapBlock[,] blocks;
    public int resolution;
    public bool debugLines = true;

    private void Update()
    {
        
    }

    public void SetTaperedCapsule (Vector2 startPoint, float startRadius, Vector2 endPoint, float endRadius, bool value)
    {
        foreach (var block in blocks)
        {

        }
    }

    public void DigTaperedCapsule(Vector2 startPoint, float startRadius, Vector2 endPoint, float endRadius)
    {
        if (blocks == null)
        {
            blocks = new MapBlock[resolution,resolution];
        }

        SetTaperedCapsule(startPoint, startRadius, endPoint, endRadius, false);
    }

    public void FillTaperedCapsule (Vector2 startPoint, float startRadius, Vector2 endPoint, float endRadius)
    {
        if (blocks == null) //if there's no dug points, no need to calculate anything lol
            return;

        SetTaperedCapsule(startPoint, startRadius, endPoint, endRadius, false);
    }
}
