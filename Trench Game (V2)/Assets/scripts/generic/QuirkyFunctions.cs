using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuirkyFunctions : MonoBehaviour
{
    public void SetTimeScale (float timeScale)
    {
        Time.timeScale = timeScale;
    }

    public void OpenLink (string link)
    {
        Application.OpenURL (link);
    }
}
