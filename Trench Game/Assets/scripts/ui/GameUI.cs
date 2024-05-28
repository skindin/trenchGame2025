using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GameUI : MonoBehaviour
{
    public float textSize = 5, scale = .015f;
    public Color textColor = Color.white;
    float deltaTime;

    private void Update()
    {
        deltaTime = Time.deltaTime;
    }

    private void OnGUI()
    {
        int width = Screen.width;
        int height = Screen.height;

        // Calculate the size of the text area
        //int rectWidth = width / 4;
        //int rectHeight = height * 2 / 100;

        // Position the rect in the lower right corner
        Rect rect = new(0,0, width, height);

        var fontSize = Mathf.RoundToInt(textSize * height * scale);

        DrawFPS(rect,fontSize);
        DrawDropTimer(rect, fontSize);
    }

    public void DrawFPS (Rect rect, int fontSize)
    {
        GUIStyle style = new();
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = fontSize;
        style.normal.textColor = textColor;

        var fps = Mathf.RoundToInt(1 / deltaTime);

        var text = $"FPS {fps}";

        GUI.Label(rect, text, style);
    }

    public void DrawDropTimer (Rect rect, int fontSize)
    {
        GUIStyle style = new();
        style.alignment = TextAnchor.UpperRight;
        style.fontSize = fontSize;
        style.normal.textColor = textColor;

        var timeLeft = ItemManager.Manager.dropInterval - ItemManager.Manager.dropTimer;

        TimeSpan timeSpan = TimeSpan.FromSeconds(timeLeft);

        // Format as "HH:mm:ss"
        string formattedTime = timeSpan.ToString(@"mm\:ss");
        //Console.WriteLine(formattedTime);  // Output: 01:01:01

        var text = $"{formattedTime} until next drop";

        GUI.Label(rect, text, style);
    }
}
