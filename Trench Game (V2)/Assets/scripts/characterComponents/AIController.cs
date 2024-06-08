using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIController : MonoBehaviour
{
    public Character character;

    private void Update()
    {
        character.Move(Random.insideUnitCircle);
    }
}
