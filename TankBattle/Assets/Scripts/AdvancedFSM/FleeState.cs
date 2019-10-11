using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FleeState : FSMState
{

    private NPCTankController npcController;

    public FleeState()
    {
        stateID = FSMStateID.Fleeing;
        curSpeed = 125f;
        curRotSpeed = 1.0f;
    }
    public override void Reason(Transform player, Transform npc)
    {
        npcController = npc.GetComponent<NPCTankController>();

        //Set the target position as the player position
        destPos = player.position;

        //Check the distance with player tank
        //When the distance is near, transition to attack state
        float dist = Vector3.Distance(npc.position, destPos);

        if (npcController.GetCurrentHealth() >= (npcController.GetMaxHealth() / 1.5) && dist < 200f)
        {
            Debug.Log(npcController.GetCurrentHealth() + "Current health " + npcController.GetMaxHealth() + " Max health");
            Debug.Log("Switch to attack state");
            npcController.SetTransition(Transition.ReachPlayer);
        } else if (dist > 800f)
        {
            npcController.SetTransition(Transition.LostPlayer);
        }
    }

    public override void Act(Transform player, Transform npc)
    {
        destPos = player.position;

        Quaternion targetRotation = Quaternion.LookRotation(npc.position - destPos);
        npc.rotation = Quaternion.Slerp(npc.rotation, targetRotation, Time.deltaTime * curRotSpeed);

        npc.Translate(Vector3.forward * Time.deltaTime * curSpeed);
    }
}
