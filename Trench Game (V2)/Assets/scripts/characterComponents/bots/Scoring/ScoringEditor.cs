using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ScoringManager))]

public class ScoringEditor : Editor
{
    ScoringManager manager;

    public override void OnInspectorGUI()
    {
        manager = (ScoringManager)target;

        base.OnInspectorGUI();

        //manager.drawSceneItemScores = EditorGUILayout.Toggle("Draw Scene Item Scores", manager.drawSceneItemScores);

        //if (manager.drawSceneItemScores)
        //{
        //    EditorGUI.indentLevel++;
        //    manager.scoreLabelOffset = EditorGUILayout.Vector2Field("Label Offset", manager.scoreLabelOffset);
        //    EditorGUI.indentLevel--;
        //}

        //DrawGunUI();
        //DrawConsumableUI();
        //DrawCharacterUI();
    }

    public void DrawGunUI ()
    {
        EditorGUILayout.LabelField("Gun");

        EditorGUI.indentLevel++;

        manager.baseGun = (Gun)EditorGUILayout.ObjectField("Base",manager.baseGun, typeof(Gun), true);

        manager.gunRoundsWeight = EditorGUILayout.FloatField("Rounds Weight", manager.gunRoundsWeight);

        manager.gunFireRateWeight = EditorGUILayout.FloatField("Fire Rate Weight", manager.gunFireRateWeight);

        manager.gunDamageRateWeight = EditorGUILayout.FloatField("Damage Rate Weight", manager.gunDamageRateWeight);

        manager.gunRangeWeight = EditorGUILayout.FloatField("Range Weight", manager.gunRangeWeight);

        EditorGUI.indentLevel--;
    }

    public void DrawConsumableUI()
    {
        EditorGUILayout.LabelField("Consumable");

        EditorGUI.indentLevel++;

        manager.baseConsumable = (Consumable)EditorGUILayout.ObjectField("Base", manager.baseConsumable, typeof(Consumable), true);

        manager.consumableHPWeight = EditorGUILayout.FloatField("HP Weight", manager.consumableHPWeight);

        manager.consumableDurationWeight = EditorGUILayout.FloatField("Duration Weight", manager.consumableDurationWeight);

        EditorGUI.indentLevel--;
    }

    public void DrawCharacterUI()
    {
        EditorGUILayout.LabelField("Character");

        EditorGUI.indentLevel++;

        manager.baseCharacter = (Character)EditorGUILayout.ObjectField("Base", manager.baseCharacter, typeof(Character), true);

        manager.characterHPWeight = EditorGUILayout.FloatField("HP Weight", manager.characterHPWeight);

        manager.characterMoveSpeedWeight = EditorGUILayout.FloatField("Move Speed Weight", manager.characterMoveSpeedWeight);

        EditorGUI.indentLevel--;
    }

    private void OnSceneGUI()
    {
        if (manager && manager.drawSceneItemScores)
        {
            GUIStyle boldStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleCenter
            };

            var allItems = FindObjectsOfType<Item>();

            foreach (var item in allItems)
            {
                Handles.Label((Vector3)manager.scoreLabelOffset + item.transform.position,
                    $"{manager.GetItemScore(item):F2}",
                    boldStyle);
            }
        }
    }
}
