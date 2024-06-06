using UnityEngine;

public class CharacterGUI : MonoBehaviour
{
    // Start is called before the first frame update

    public Character character;
    public float amoTextSize = 5, itemTextSize = 2;
    public Color plainText = Color.white, backgroundColor = Color.white, importantColor = Color.white;
    public Vector2 infoBoxOffset;
    public float scale = 100, infoBoxPadding = 10, screenPadding = 10;
    Texture2D infoBoxTexture;

    void OnGUI()
    {
        DrawAmoGUI();
        DrawNearbyItems();

        infoBoxTexture = new Texture2D(1,1);
        //infoBoxTexture.SetPixels
    }

    public void DrawAmoGUI ()
    {
        //if (!character.gun) return; //technically should still draw the reserve but whatever

        var scaleFactor = Screen.height * scale;
        GUIStyle style = new();

        style.normal.textColor = plainText;

        style.fontSize = Mathf.RoundToInt(amoTextSize * scaleFactor);
        style.alignment = TextAnchor.LowerRight;

        var padding = Mathf.RoundToInt(screenPadding * scaleFactor);

        style.padding = new (padding, padding, padding, padding);

        int width = Screen.width;
        int height = Screen.height;

        // Calculate the size of the text area
        int rectWidth = width / 4;
        int rectHeight = height * 2 / 100;

        // Position the rect in the lower right corner
        Rect rect = new (width - rectWidth, height - rectHeight, rectWidth, rectHeight);

        string text = "";
        if (character.gun)
        {
            if (character.gun.reloading)
                text += "(Reloading)\n";
            text += character.gun.GetInfo("\n");
        }

        if (character.reserve)
        {
            var reserveInfo = character.reserve.GetInfo("\n");
            string reserveText = string.Join("\n", reserveInfo);
            text += $"\n\n{reserveText}";
        }

        GUI.Label(rect, text, style);
    }

    public void DrawNearbyItems ()
    {
        GUI.backgroundColor = backgroundColor;

        GUIStyle style = new();

        style.normal.textColor = plainText;

        var scaleFactor = Screen.height * scale;
        style.alignment = TextAnchor.UpperCenter;
        style.fontSize = Mathf.RoundToInt(itemTextSize * scaleFactor);
        style.normal.background = infoBoxTexture;
        var padding = Mathf.RoundToInt(infoBoxPadding * scaleFactor);
        style.padding = new(padding, padding, padding, padding);

        foreach (var item in character.inventory.withinRadius)
        {
            if (item == character.inventory.closestItem) continue;
                DrawTextBox(item.model.name, item.transform.position, style, scaleFactor);
        }

        if (character.inventory.closestItem != null)
        {
            style.normal.textColor = importantColor;
            DrawItemUI(character.inventory.closestItem, style, scaleFactor);
        }
    }

    public void DrawItemUI (Item item, GUIStyle style, float scaleFactor)
    {
        var info = item.GetInfo("\n");

        DrawTextBox(info, item.transform.position, style, scaleFactor);
    }

    public void DrawTextBox (string infoString, Vector3 pos,  GUIStyle style, float scaleFactor)
    {
        var size = style.CalcSize(new GUIContent(infoString));

        var screenPos = Camera.main.WorldToScreenPoint(pos);

        //var guiPos = new Vector2(screenPos.x, Screen.height - screenPos.y) - (size / 2) + (infoBoxOffset * scaleFactor);
        var guiPos = new Vector2(screenPos.x - size.x / 2, Screen.height - screenPos.y) + (infoBoxOffset * scaleFactor);

        GUI.Box(new Rect(guiPos.x, guiPos.y, size.x, size.y), infoString, style);
    }
}
