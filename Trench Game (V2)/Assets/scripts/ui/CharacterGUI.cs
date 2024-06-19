using UnityEngine;
using System;

public class CharacterGUI : MonoBehaviour
{
    // Start is called before the first frame update

    CharacterGUI main;

    public CharacterGUI Main
    { 
        get
        {
            if (!main)
            {
                main = GetComponent<CharacterGUI>();
            }

            return main;
        }

        set
        {
            main = value;
        }
    }

    public Character character;
    public float amoTextSize = 5, itemTextSize = 2;
    public Color plainTextColor = Color.white, backgroundColor = Color.white, importantColor = Color.white;
    public Vector2 itemBoxOffset, characterBoxOffset;
    public float scale = 100, infoBoxPadding = 10, screenPadding = 10;
    Texture2D infoBoxTexture;

    private void Awake()
    {
        infoBoxTexture = new Texture2D(1, 1);
    }

    void OnGUI()
    {
        var scaleFactor = Screen.height * scale;

        var boxStyle = GetBoxStyle();

        DrawNearbyItemsUI(boxStyle,scaleFactor);
        DrawAllCharacterUI(boxStyle,scaleFactor);

        DrawAssignedCharacterUI(scaleFactor);
    }

    public GUIStyle GetBoxStyle ()
    {
        GUI.backgroundColor = backgroundColor;

        GUIStyle style = new();

        style.normal.textColor = plainTextColor;

        var scaleFactor = Screen.height * scale;
        style.alignment = TextAnchor.UpperCenter;
        style.fontSize = Mathf.RoundToInt(itemTextSize * scaleFactor);
        style.normal.background = infoBoxTexture;
        var padding = Mathf.RoundToInt(infoBoxPadding * scaleFactor);
        style.padding = new(padding, padding, padding, padding);

        return style;
    }

    public void DrawAssignedCharacterUI (float scaleFactor)
    {
        if (!character) return;

        GUIStyle style = new();

        style.normal.textColor = plainTextColor;

        style.fontSize = Mathf.RoundToInt(amoTextSize * scaleFactor);
        style.alignment = TextAnchor.LowerRight;

        var padding = Mathf.RoundToInt(screenPadding * scaleFactor);

        style.padding = new (padding, padding, padding, padding);

        int width = Screen.width;
        int height = Screen.height;

        // Calculate the size of the text area
        //int rectWidth = width / 4;
        //int rectHeight = height * 2 / 100;

        // Position the rect in the lower right corner
        Rect rect = new (0, 0, width, height);

        string text = "";
        if (character.gun)
        {
            if (character.gun.reloading)
                text += "(Reloading)\n";
            text += character.gun.InfoString("\n");
        }

        if (character.reserve)
        {
            var reserveInfo = character.reserve.GetInfo("\n");
            string reserveText = string.Join("\n", reserveInfo);
            text += $"\n\n{reserveText}";
        }

        GUI.Label(rect, text, style);

        style.alignment = TextAnchor.LowerCenter;

        GUI.Label(rect, character.InfoString(), style);
    }

    public void DrawNearbyItemsUI (GUIStyle style, float scaleFactor)
    {
        if (!character) return;

        foreach (var item in character.inventory.withinRadius)
        {
            if (item == character.inventory.SelectedItem) continue;

            if (item.Chunk == null)
                throw new Exception($"{(transform.parent? $"{transform.parent.gameObject} ":"unheld ")}{item.name} is not in a chunk");

            if (!item.gameObject.activeSelf)
                throw new Exception($"{item.name} is disabled");

                DrawTextBox(item.model.name, item.transform.position, style, itemBoxOffset * scaleFactor);
        }

        if (character.inventory.SelectedItem != null)
        {
            style.normal.textColor = importantColor;
            DrawItemUI(character.inventory.SelectedItem, style, scaleFactor);
            style.normal.textColor = plainTextColor;
        }
    }

    public void DrawItemUI (Item item, GUIStyle style, float scaleFactor)
    {
        var info = item.InfoString("\n");

        DrawTextBox(info, (Vector2)item.transform.position, style, itemBoxOffset * scaleFactor);
    }

    public void DrawAllCharacterUI (GUIStyle style, float scaleFactor)
    {

        foreach (var chunk in ChunkManager.Manager.chunks)
        {
            if (chunk == null) continue;

            foreach (var character in chunk.characters)
            {
                if (character == this.character)
                    continue;

                DrawCharacterUI(character, style, scaleFactor);
            }
        }
    }

    public void DrawCharacterUI (Character character, GUIStyle style, float scaleFactor)
    {
        DrawTextBox(character.InfoString(), (Vector2)character.transform.position, style, characterBoxOffset * scaleFactor);
    }

    public void DrawTextBox (string infoString, Vector2 pos,  GUIStyle style, Vector2 offset = default)
    {
        var size = style.CalcSize(new GUIContent(infoString));

        var screenPos = Camera.main.WorldToScreenPoint(pos);

        //var guiPos = new Vector2(screenPos.x, Screen.height - screenPos.y) - (size / 2) + (infoBoxOffset * scaleFactor);
        var guiPos = new Vector2(screenPos.x - size.x / 2, Screen.height - screenPos.y) + new Vector2(offset.x,-offset.y);

        GUI.Box(new Rect(guiPos.x, guiPos.y, size.x, size.y), infoString, style);
    }
}
