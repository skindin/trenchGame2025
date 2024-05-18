using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LetThereBeLag : MonoBehaviour
{
    public TrenchDigger digger;
    public bool makeALottaTrenches = true, displayFPS = true;
    public int totalTrenches;
    int currentCount = 0;
    float deltaTime;

    private void Update()
    {
        while (makeALottaTrenches && currentCount < totalTrenches)
        {
            digger.DigTrench(transform.position, 0);
            digger.StopDigging();
            currentCount++;
        }

        deltaTime = Time.deltaTime;
    }

    void OnGUI()
    {
        if (!displayFPS) return;

        int width = Screen.width, height = Screen.height;
        GUIStyle style = new GUIStyle();

        Rect rect = new Rect(0, 0, width, height * 2 / 100);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = height * 10 / 100;
        style.normal.textColor = Color.red;
        float msec = deltaTime * 1000.0f;
        float fps = 1.0f / deltaTime;
        string text = string.Format("{1:0.} fps", msec, fps);
        GUI.Label(rect, text, style);
    }
}
