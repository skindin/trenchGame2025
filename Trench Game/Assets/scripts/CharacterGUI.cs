using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterGUI : MonoBehaviour
{
    // Start is called before the first frame update

    public Character character;
    public int textSize = 10;
    public Color color = Color.white;

    void OnGUI()
    {
        int width = Screen.width;
        int height = Screen.height;
        GUIStyle style = new GUIStyle();

        // Calculate the size of the text area
        int rectWidth = width / 4;
        int rectHeight = height * 2 / 100;

        // Position the rect in the lower right corner
        Rect rect = new (width - rectWidth, height - rectHeight, rectWidth, rectHeight);

        style.alignment = TextAnchor.LowerRight;
        style.fontSize = height * textSize / 100;
        style.normal.textColor = color;

        string gunText = $"Gun {character.gun.rounds}/{character.gun.GunModel.maxRounds}";

        var amoType = character.gun.GunModel.amoType;
        var reserveRounds = character.reserve.GetAmoAmount(amoType);
        var reserveMaxRounds = character.reserve.GetReserve(amoType).maxRounds;
        string reserveText = $"Reserve {reserveRounds}/{reserveMaxRounds}";

        string text = $"{gunText}\n{reserveText}";
        GUI.Label(rect, text, style);
    }
}
