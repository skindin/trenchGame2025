using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TintSprite : MonoBehaviour
{
    public Color tintColor = Color.white;
    public SpriteRenderer sprite;

    Color ogColor;

    private void Awake()
    {
        ogColor = sprite.color;
    }

    public void ApplyTint (float ratio)
    {
        var opaqueTintColor = tintColor;
        opaqueTintColor.a = 1;

        sprite.color = Color.Lerp(ogColor, opaqueTintColor, tintColor.a * ratio);
    }

    public void ApplyTint (bool toggle)
    {
        ApplyTint(toggle ? 1 : 0);
    }

    public void ReverseApplyTint (bool toggle)
    {
        ApplyTint(!toggle);
    }
}
