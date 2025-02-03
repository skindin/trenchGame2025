using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Chunks;

public class Minimap : MonoBehaviour
{
    public Image charIconPrfb, itemDropIcon;
    public TextMeshProUGUI dropTimeText;
    public string timeTextSuffix = "s until drop";
    public List<Image> charIcons = new();
    public Color defaultColor = Color.white, playerColor = Color.white;
    public RectTransform mapImage;
    public ObjectPool<Image> characterIconPool;
    public bool showItemDrops = true, debugLines = false;//, logPosRatios = false;

    private void Awake()
    {
        characterIconPool = new ObjectPool<Image>(characterIconPool.minPooled, characterIconPool.maxPooled, () => Instantiate(charIconPrfb.gameObject, transform).GetComponent<Image>());
    }

    private void Update()
    {
        UpdateCharIcons();

        if (showItemDrops)
            UpdateDropArea();

        dropTimeText.gameObject.SetActive(showItemDrops);
        itemDropIcon.gameObject.SetActive(showItemDrops);
    }

    void UpdateCharIcons ()
    {
        while (charIcons.Count > CharacterManager.Manager.active.Count)
        {
            characterIconPool.AddToPool(charIcons[0]);
            charIcons[0].gameObject.SetActive(false);
            charIcons.RemoveAt(0);

            //goto SkipNewIcons;
        }

        while (charIcons.Count < CharacterManager.Manager.active.Count)
        {
            var newIcon = characterIconPool.GetFromPool();
            newIcon.gameObject.SetActive(true);
            charIcons.Add(newIcon);
        }

        //SkipNewIcons:

        var mapMin = (Vector2)mapImage.position + (mapImage.rect.min * mapImage.lossyScale);
        var scaleFactor = new Vector2(mapImage.rect.width, mapImage.rect.height) * mapImage.lossyScale;

        if (debugLines)
        {
            GeoUtils.DrawBoxPosSize(mapImage.position, scaleFactor, Color.red);
        }

        for (int i = 0; i < charIcons.Count; i++)
        {
            var icon = charIcons[i];
            var character = CharacterManager.Manager.active[i];
            var posRatio = ChunkManager.GetPosRatio(character.transform.position);

            icon.gameObject.SetActive(character.gameObject.activeInHierarchy);

            icon.transform.position = posRatio * scaleFactor + mapMin;

            icon.color = (character.Type == Character.CharacterType.localPlayer) ? playerColor : character.clan.color;

            //if (logPosRatios && character.Type == Character.CharacterType.localPlayer)
            //{
            //    Debug.Log(posRatio);
            //}
        }
    }

    void UpdateDropArea ()
    {
        var mapMin = (Vector2)mapImage.position + mapImage.rect.min;
        var scaleFactor = new Vector2(mapImage.rect.width, mapImage.rect.height);

        var centerPosRatio = ChunkManager.GetPosRatio(SpawnManager.Manager.dropAreaCenter);
        itemDropIcon.transform.position = centerPosRatio * scaleFactor + mapMin;

        var sizeDelta = itemDropIcon.rectTransform.sizeDelta;

        var radiusRatio = SpawnManager.Manager.dropAreaRadius / ChunkManager.Manager.worldSize;

        sizeDelta.x = radiusRatio * scaleFactor.x;
        sizeDelta.y = radiusRatio * scaleFactor.y;

        itemDropIcon.rectTransform.sizeDelta = sizeDelta * 2;

        dropTimeText.text = "" + Mathf.CeilToInt(SpawnManager.Manager.TimeToNextDrop) + timeTextSuffix;
    }
}
