using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GameUI : MonoBehaviour
{
    public float textSize = 5, scale = .015f;
    public Color textColor = Color.white;
    float deltaTime;
    public float padding = 10;

    private void Update()
    {
        deltaTime = Time.unscaledDeltaTime;
    }

    private void OnGUI()
    {
        //foreach (var character in CharacterManager.Manager.active)
        //{
        //    CharacterGUI.Main.DrawTextBox
        //}

        int width = Screen.width;
        int height = Screen.height;

        // Calculate the size of the text area
        //int rectWidth = width / 4;
        //int rectHeight = height * 2 / 100;

        // Position the rect in the lower right corner
        Rect rect = new(0,0, width, height);

        var fontSize = Mathf.RoundToInt(textSize * height * scale);
        var padding = Mathf.RoundToInt(this.padding * height * scale);

        DrawFPS(rect,fontSize, padding);
        DrawTimers(rect, fontSize, padding);
    }

    public void DrawFPS (Rect rect, int fontSize, int padding)
    {
        GUIStyle style = new();
        style.alignment = TextAnchor.UpperCenter;
        style.fontSize = fontSize;
        style.normal.textColor = textColor;
        style.padding = new(padding, padding, padding, padding);

        var fps = Mathf.RoundToInt(1 / deltaTime);
        var bulletCount = ProjectileManager.Manager.activeBullets.Count;
        //var secsPerBullet = 1/deltaTime/ bulletCount; //didn't really help

        //if (secsPerBullet == Mathf.Infinity)
        //    secsPerBullet = 0;

        var text = $"FPS({fps}) bullets({bulletCount})";

        GUI.Label(rect, text, style);
    }

    public void DrawTimers (Rect rect, int fontSize, int padding)
    {
        if (!ItemManager.Manager) return;

        GUIStyle style = new();
        style.alignment = TextAnchor.UpperRight;
        style.fontSize = fontSize;
        style.normal.textColor = textColor;
        style.padding = new(padding, padding, padding, padding);

        var timeLeft = ItemManager.Manager.TimeToNextDrop;

        string itemDropTimeText = GetTimeText(ItemManager.Manager.TimeToNextDrop) + " to item drop";
        GUI.Label(rect,itemDropTimeText,style);
        //string squadSpawnTimeText = GetTimeText(CharacterManager.Manager.TimeToSquadSpawn) + " to squad spawn";

        //var text = itemDropTimeText + "\n" + squadSpawnTimeText;

        //GUI.Label(rect, text, style);

    }

    public string GetTimeText (float seconds)
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);

        // Format as "HH:mm:ss"
        string timeText = timeSpan.ToString(@"mm\:ss");

        return timeText;
    }
}
