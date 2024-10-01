using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SpriteAnimAssist : MonoBehaviour
{
    public UnityEvent onMove, onMoveLeft, onMoveRight, onStop;
    public SpriteRenderer sprite;//, shadowSprite;

    Coroutine moveCheckRoutine;

    //private void Update()
    //{
    //    if (shadowSprite)
    //    {
    //        shadowSprite.sprite = sprite.sprite; //doesn't work for some reason
    //    }
    //}

    public void Move (Vector2 direction)
    {
        if (direction.magnitude <= 0)
        {
            onStop.Invoke ();
            return;
        }

        if (moveCheckRoutine != null)
        {
            StopCoroutine (moveCheckRoutine);
        }
        moveCheckRoutine = StartCoroutine(CheckForStop());

        onMove.Invoke();

        if (direction.x > 0)
        {
            onMoveRight.Invoke();
        }
        else if (direction.x < 0)
        {
            onMoveLeft.Invoke();
        }

        IEnumerator CheckForStop ()
        {
            yield return null;

            onStop.Invoke();
        }
    }

    public void FlipX (bool flip)
    {
        var scale = sprite.transform.localScale;
        var xAbs = Mathf.Abs(scale.x);
        scale.x = flip ? xAbs : -xAbs;
        sprite.transform.localScale = scale;
    }
}
