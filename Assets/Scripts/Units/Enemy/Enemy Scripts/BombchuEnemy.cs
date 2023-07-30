using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class BombchuEnemy : EnemyUnit
{
    protected override void GetNextWayPoint()
    {
        int wayPointsCheck = BBAWayPointsManager.instance.CheckForEnemyPosition(waypointIndex);

        if (wayPointsCheck == 1)
        {
            BaseManager.instance.ChangeHealth(-maxHealthPoints);
            WaveManager.instance.EnemyKilled();
            NetworkServer.Destroy(gameObject);
            return;
        }

        else if (wayPointsCheck == 1)
        {
            WaveManager.instance.EnemyKilled();
            NetworkServer.Destroy(gameObject);
        }

        waypointIndex++;
        target = WayPointsManager.instance.points[waypointIndex];

        var lookAtWaypoint = new Vector3(target.position.x, transform.position.y, target.position.z);

        transform.LookAt(lookAtWaypoint, Vector3.up);
    }
}