using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class SimpleFSMScouting : FSM
{
    //Scouting stuff
    //The radius in which the scout position are
    public float scoutRadius;
    //The interval between scout position
    public float scoutTimer;

    private Transform target;

    private NavMeshAgent agent;

    private float timer;

    public TankCoordinator coordinator;

    public enum FSMState
    {
        None,
        Patrol,
        Chase,
        Attack,
        Dead,
        Scouting,
    }

    //Current state that the NPC is reaching
    public FSMState curState;

    //Speed of the tank
    private float curSpeed;

    //Tank Rotation Speed
    private float curRotSpeed;

    //Bullet
    public GameObject Bullet;

    //Whether the NPC is destroyed or not
    private bool bDead;
    public int health;


    //Initialize the Finite state machine for the NPC tank
    protected override void Initialize()
    {
        coordinator = FindObjectOfType<TankCoordinator>();
        curState = FSMState.Scouting;

        bDead = false;

        //Get the turret of the tank
        turret = gameObject.transform.GetChild(0).transform;
        bulletSpawnPoint = turret.GetChild(0).transform;

        agent = GetComponent<NavMeshAgent>();
        timer = scoutTimer;
    }

    //Update each frame
    protected override void FSMUpdate()
    {
        switch (curState)
        {
            case FSMState.Scouting: UpdateScoutingState(); break;
        }

        //Update the time
        elapsedTime += Time.deltaTime;

        //Go to dead state is no health left
        if (health <= 0)
            curState = FSMState.Dead;
    }

    //Once the timer reaches the scoutTimer it will calculate a new point to scout.
    //The middlepoint of all the tanks gets calculated. The timer gets reset. 
    //Agent destination gets set 
    protected void UpdateScoutingState()
    {
        timer += Time.deltaTime;

        if (timer >= scoutTimer)
        {
            Vector3 middlePoint = Vector3.zero;
            foreach (GameObject tank in coordinator.alliedTanks)
            {
                middlePoint += tank.transform.position;
            }
            middlePoint = middlePoint / coordinator.alliedTanks.Length;
            Vector3 newPos = RandomNavSphere(middlePoint, scoutRadius, -1);
            agent.SetDestination(newPos);
            timer = 0;
        }

        //Not used atm
        //var EnemyList = GameObject.FindGameObjectsWithTag("Red");
        //foreach (GameObject enemy in EnemyList)
        //{
        //    var dist = Vector3.Distance(transform.position, enemy.transform.position);
        //    if (dist <= 150f)
        //    {
        //        Debug.Log("found enemy");
        //    }
        //}
    }

    //Vector3 origin    => The calculated middle of the allied tanks
    //float dist        => The distance from the origin wherein a scout position will be found
    //int layermask     => This is needed if there are different layers the tank might not be able to get to
    //Vector3 output    => A position from origin within dist on layermask
    public static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist;

        randDirection += origin;

        NavMeshHit navHit;

        NavMesh.SamplePosition(randDirection, out navHit, dist, layermask);

        return navHit.position;
    }
    
    //Check if there are any Enemies within scout radius, if so change to attacking state
    private void OnTriggerStay(Collider other)
    {
        foreach (GameObject Enemy in coordinator.enemyTanks)
        {
            if (other.tag == Enemy.tag)
            {
                Debug.Log(other);
                //Todo implement change to state
                Debug.Log("Go to attack state");
            }
        }
    }
}
