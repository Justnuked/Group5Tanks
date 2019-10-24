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

    private bool hasEnemyPosition;



    public enum FSMState
    {
        None,
        Patrol,
        Chase,
        Attack,
        Dead,
        Scouting,
        Retreat,
        Regrouping,
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

    public Transform enemy = null;

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
            case FSMState.Regrouping: UpdateRegroupingState(); break;

        }


        //Update the time
        elapsedTime += Time.deltaTime;

        //Go to dead state is no health left
        if (health <= 0)
            curState = FSMState.Dead;
    }

    void UpdateRegroupingState()
    {
        float dist = Vector3.Distance(transform.position, enemy.position);
        Quaternion targetRotation = Quaternion.LookRotation(enemy.position - transform.position);
        if (!hasEnemyPosition)
        {
            var pos = (transform.position - enemy.position);
            Debug.Log(pos);
            agent.SetDestination(pos);
            hasEnemyPosition = true;
        }
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
        float dist = Vector3.Distance(transform.position, enemy.position);
        Debug.Log(dist);
        if (!hasEnemyPosition)
        {
            Vector3 newPos = RandomNavSphere(enemy.position, scoutRadius, -1);
            agent.SetDestination(newPos);
            hasEnemyPosition = true;
        }

        Quaternion turretRotation = Quaternion.LookRotation(enemy.position - turret.position);
        turret.rotation = Quaternion.Slerp(turret.rotation, turretRotation, Time.deltaTime * turretRotationSpeed);

        if (dist <= attackRange)
        {
            if (enemy == null)
            {
                return;
            }

            //Shoot the bullets
            if (dist <= attackRange)
            {
                ShootBullet();
                curState = FSMState.Scouting;
                Debug.Log("Back to Scouting");
            }
        }
    }

    protected void UpdateRetreatState()
    {
        float dist = Vector3.Distance(transform.position, enemy.position);
        agent.SetDestination(enemy.position - gameObject.transform.position);

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
    //Check if there are any Enemies within scout radius, if so change to attacking state
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<Terrain>() || other.CompareTag("Bullet"))
        {
            Debug.Log("terrain");
            return;
        }

        //foreach (GameObject Ally in coordinator.alliedTanks)
        //{
        if (other.tag != "Ally")
        {
            enemy = other.gameObject.transform;
            Debug.Log(enemy.name);
            //Switching to attack state
            Debug.Log("Go to regroup state");
            hasEnemyPosition = false;
            curState = FSMState.Regrouping;
        }
        //}
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
