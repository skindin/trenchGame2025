using UnityEngine;

public class CharacterGUI : MonoBehaviour
{
    // Start is called before the first frame update

    public Character character;
    public float amoTextSize = 5, itemTextSize = 2;
    public Color plainText = Color.white, backgroundColor = Color.white, importantColor = Color.white;
    public Vector2 infoBoxOffset;
    public float scale = 100;
    Texture2D infoBoxTexture;

    void OnGUI()
    {
        DrawAmoGUI();
        DrawNearbyItems();

        infoBoxTexture = new Texture2D(100, 100);
        //infoBoxTexture.SetPixels
    }

    public void DrawAmoGUI ()
    {
        var scaleFactor = Screen.height * scale;
        GUIStyle style = new();

        style.normal.textColor = plainText;

        style.fontSize = Mathf.RoundToInt(amoTextSize * scaleFactor);
        style.alignment = TextAnchor.LowerRight;

        int width = Screen.width;
        int height = Screen.height;

        // Calculate the size of the text area
        int rectWidth = width / 4;
        int rectHeight = height * 2 / 100;

        // Position the rect in the lower right corner
        Rect rect = new (width - rectWidth, height - rectHeight, rectWidth, rectHeight);


        string gunText = $"Gun {character.gun.rounds}/{character.gun.GunModel.maxRounds}";

        var amoType = character.gun.GunModel.amoType;
        var reserveRounds = character.reserve.GetAmoAmount(amoType);
        var reserveMaxRounds = character.reserve.GetReserve(amoType).maxRounds;
        string reserveText = $"Reserve {reserveRounds}/{reserveMaxRounds}";

        string text = $"{gunText}\n{reserveText}";
        GUI.Label(rect, text, style);
    }

    public void DrawNearbyItems ()
    {
        GUI.backgroundColor = backgroundColor;

        GUIStyle style = new();

        style.normal.textColor = plainText;

        var scaleFactor = Screen.height * scale;
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = Mathf.RoundToInt(itemTextSize * scaleFactor);
        style.normal.background = infoBoxTexture;

        foreach (var item in character.inventory.withinRadius)
        {
            if (item == character.inventory.closestItem) continue;
            DrawItemUI(item,style,scaleFactor);
        }

        if (character.inventory.closestItem != null)
        {
            style.normal.textColor = importantColor;
            DrawItemUI(character.inventory.closestItem, style, scaleFactor);
        }
    }

    public void DrawItemUI (Item item, GUIStyle style, float scaleFactor)
    {
        var infoArray = item.GetInfo();
        var infoString = string.Join(" ", infoArray);

        var size = style.CalcSize(new GUIContent(infoString));

        var screenPos = Camera.main.WorldToScreenPoint(item.transform.position);

        var guiPos = new Vector2(screenPos.x, Screen.height - screenPos.y) - (size / 2) + (infoBoxOffset * scaleFactor);

        GUI.Box(new Rect(guiPos.x, guiPos.y, size.x, size.y), infoString, style);
    }
}
