using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class ClubMoblin : EnemyUnit
{
    [SerializeField] List<Unit> unitsInRange = new List<Unit>();

    [SerializeField] int abilityCooldown;


    protected override void Start()
    {
        base.Start();

        if (!isServer) return;

        StartCoroutine(SmashAbility());
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
    }

    IEnumerator SmashAbility()
    {
        while (true)
        {
            float originalSpeed = speed;

            yield return new WaitForSeconds(abilityCooldown);

            speed = 0;

            animManager.AttackingAnim(0.1f);

            yield return new WaitForSeconds(2.6f);

            for (int i = 0; i < unitsInRange.Count; i++)
            {
                if (unitsInRange[i] == null) unitsInRange.RemoveAt(i);

                StartCoroutine(unitsInRange[i].StunnedEffect(10));
            }

            yield return new WaitForSeconds(1);

            speed = originalSpeed;
        }
    }

    /// <summary>
    /// OnTriggerEnter is called when the Collider other enters the trigger.
    /// </summary>
    /// <param name="other">The other Collider involved in this collision.</param>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Unit"))
        {
            unitsInRange.Add(other.GetComponent<Unit>());
        }
    }

    /// <summary>
    /// OnTriggerExit is called when the Collider other has stopped touching the trigger.
    /// </summary>
    /// <param name="other">The other Collider involved in this collision.</param>
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Unit"))
        {
            unitsInRange.Add(other.GetComponent<Unit>());
        }
    }
}
