using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EvadeState : FSMState
{
    private float timePassed;
    private Transform otherTank;
    public EvadeState()
    {
        stateID = FSMStateID.Evading;
        curSpeed = 125f;
        curRotSpeed = 1.0f;
    }

    public override void Reason(Transform player, Transform npc)
    {
        if (timePassed > 0.5f)
        {
            timePassed = 0f;
            npc.GetComponent<NPCTankController>().SetTransition(Transition.LostPlayer);
        }
    }

    public override void Act(Transform player, Transform npc)
    {
    }

    public override void ActWithoutPlayer(Transform lhs, Transform rhs)
    {
        if (otherTank == null)
        {
            otherTank = lhs;
        }
        else if (!otherTank.Equals(lhs))
        {
            otherTank = lhs;
        }

        Vector3 obstaclePos = otherTank.position;

        Quaternion targetRotation = Quaternion.LookRotation(rhs.transform.position - obstaclePos);
        rhs.rotation = Quaternion.Slerp(rhs.rotation, targetRotation, Time.deltaTime * curRotSpeed);

        rhs.Translate(Vector3.forward * Time.deltaTime * curSpeed);

        timePassed += Time.deltaTime;
    }
}
