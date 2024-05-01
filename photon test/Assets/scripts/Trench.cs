using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Trench : MonoBehaviourPunCallbacks
{
    public LineRenderer line;
    public float width;

    private void Start()
    {
        TrenchManager.instance.trenches.Add(this);
    }

    private void Update()
    {
        if (!photonView.IsMine) return;

        Vector2[] points = new Vector2[line.positionCount];
        for (var i = 0; i < line.positionCount; i++)
        {
            var point = line.GetPosition(i);
            points[i] = point;
        }

        photonView.RPC("SyncLine", RpcTarget.Others, points, line.widthMultiplier);

        List<float> test = new();
    }

    //too many trenches seems to overload a clients output
    //this could probably be solved by not running it 60 times per second lol
    //also, players shouldn't be 'owners' of trenches, atleast not after they stop digging

    [PunRPC]
    public void SyncLine (Vector2[] points, float width)
    {
        line.positionCount = points.Length;

        for (var i = 0; i < points.Length; i++)
        {
            var data = points[i];

            Vector2 point = new(data[0], data[1]);

            line.SetPosition(i, point);
        }

        line.widthMultiplier = width;
    }
}
