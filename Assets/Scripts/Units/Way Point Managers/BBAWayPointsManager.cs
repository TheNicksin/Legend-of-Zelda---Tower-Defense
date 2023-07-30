using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

public class BBAWayPointsManager : NetworkBehaviour
{
    public Transform[] points;
    public static BBAWayPointsManager instance;

    public override void OnStartServer()
    {
        base.OnStartServer();

        instance = this;

        points = new Transform[transform.childCount];

        for (int i = 0; i < points.Length; i++)
        {
            points[i] = transform.GetChild(i);
        }
    }

    int currentTarget = 0;
    public UnityEvent takeDownTarget1;
    public UnityEvent takeDownTarget2;

    // return 0 = false
    // return 1 = true with damaging base
    // return 2 = true without damaging base
    public int CheckForEnemyPosition(int index)
    {
        Debug.Log(index);

        switch(index)
        {
            // final target
            case 4:
                return 1;

            case 3:
                if (currentTarget == 1)
                {
                    RpcTakeDownTarget2();
                    currentTarget++;
                    return 2;
                }
                
                else return 0;
            case 2:
                if (currentTarget == 0)
                {
                    RpcTakeDownTarget1();
                    currentTarget++;
                    return 2;
                }
                
                else return 0;

            default:
                return 0;
        }
    }

    [ClientRpc]
    void RpcTakeDownTarget1()
    {
        takeDownTarget1.Invoke();
    }

    [ClientRpc]
    void RpcTakeDownTarget2()
    {
        takeDownTarget2.Invoke();
    }
}