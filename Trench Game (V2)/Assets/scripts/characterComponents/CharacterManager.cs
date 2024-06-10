using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterManager : MonoBehaviour
{
    public ObjectPool<Character> pool;
    public List<Character> active = new();
    public Character prefab;
    public Transform container;

    static CharacterManager manager;
    public static CharacterManager Manager
    {
        get
        {
            if (manager == null)
            {
                manager = FindObjectOfType<CharacterManager>();
                //if (manager == null)
                //{
                //    GameObject go = new GameObject("Bullet");
                //    manager = go.AddComponent<BulletManager>();
                //    DontDestroyOnLoad(go);
                //}
            }
            return manager;
        }
    }

    private void Awake()
    {
        SetupPool();
    }

    void SetupPool ()
    {
        pool.newFunc = () => Instantiate(prefab, container).GetComponent<Character>();

        pool.disableAction = character =>
        {
            character.gameObject.SetActive(false);
            //character.transform.parent = container;
        };

        pool.resetAction = character =>
        {
            character.ResetCharacter();
            character.gameObject.SetActive(true);
            //item.transform.parent = container;
        };

        pool.removeAction = character => Destroy(character.gameObject, 0.0001f);
    }

    public Character NewCharacter (Vector2 pos, Character.CharacterType type)
    {
        var newCharacter = pool.GetFromPool();

        newCharacter.transform.position = pos;
        newCharacter.UpdateChunk();

        newCharacter.Type = type;

        active.Add(newCharacter);

        return newCharacter;
    }

    public void RemoveCharacter(Character character)
    {
        active.Remove(character);

        pool.AddToPool(character);
        character.Chunk = null;
    }
}
