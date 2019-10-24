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
        Retreat,
    }

    //Current state that the NPC is reaching
    public FSMState curState;

    //Speed of the tank
    private float curSpeed;

    //Tank Rotation Speed
    private float curRotSpeed;

    //Bullet
    public GameObject Bullet;

    //Values determined in the tankbattle ruleset
    private float shootingRate = 3.0f;

    [SerializeField]
    private float turretRotationSpeed = 1.5f;
    [SerializeField]
    private float attackRange = 150.0f;
 
    private float spottingRange = 300.0f;

    //Whether the NPC is destroyed or not
    private bool bDead;
    public int health;

    public Transform ENEMY = null;

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
            case FSMState.Attack: UpdateAttackState(); break;
            case FSMState.Retreat: UpdateRetreatState(); break;
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

    protected void UpdateAttackState()
    { 
        float dist = Vector3.Distance(transform.position, ENEMY.position);
        if (dist <= attackRange)
        {
            if (ENEMY == null)
            {
                return;
            }

            //Turns the turret towards target
            Quaternion turretRotation = Quaternion.LookRotation(ENEMY.position - turret.position);
            turret.rotation = Quaternion.Slerp(turret.rotation, turretRotation, Time.deltaTime * turretRotationSpeed);

            //Shoot the bullets
            ShootBullet();
            curState = FSMState.Retreat;
        }
    }

    protected void UpdateRetreatState()
    {
        float dist = Vector3.Distance(transform.position, ENEMY.position);
        agent.SetDestination(ENEMY.position - gameObject.transform.position);

        if (dist <= attackRange)
        {
            curState = FSMState.Attack;
            Debug.Log("attackSTate through retreat");
        }
        if (dist > attackRange)
        {
            curState = FSMState.Scouting;
            Debug.Log("Scouting through retreat");
        }
        Debug.Log(agent.destination);
        Debug.Log("In Retreat");
        curState = FSMState.Scouting;
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
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<Terrain>() || other.CompareTag("Bullet"))
        {
            Debug.Log("terrain");
            return;
        }

        foreach (GameObject Ally in coordinator.alliedTanks)
        {
            if (other.tag != Ally.tag)
            {
                ENEMY = other.gameObject.transform;
                Debug.Log(ENEMY.name);
                //Switching to attack state
                Debug.Log("Go to attack state");
                curState = FSMState.Attack;
            }
        }
    }

    private void ShootBullet()
    {
        if (elapsedTime >= shootingRate)
        {
            //Shoot the bullet
            Instantiate(Bullet, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
            elapsedTime = 0.0f;
        }
    }
}
