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
    //The radius where the tanks will move towards when attacking
    public float attackPointRadius;

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
        Reload,
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

    private float platoonDist;
    //Whether the NPC is destroyed or not
    private bool bDead;
    public int health;

    static public Transform enemy = null;

    //Initialize the Finite state machine for the NPC tank
    protected override void Initialize()
    {
        coordinator = FindObjectOfType<TankCoordinator>();
        curState = FSMState.Scouting;
        
        platoonDist = coordinator.platoonDist;

        bDead = false;

        //Get the turret of the tank
        turret = gameObject.transform.GetChild(0).transform;
        bulletSpawnPoint = turret.GetChild(0).transform;

        agent = GetComponent<NavMeshAgent>();
        agent.speed = curSpeed;
        timer = scoutTimer;
    }

    public void CallRetreating()
    {
        if (curState != FSMState.Retreat)
        {
            foreach (GameObject ally in coordinator.alliedTanks)
            {

                SimpleFSMScouting allyscript = ally.gameObject.GetComponent<SimpleFSMScouting>();
                allyscript.CallRetreating();
                curState = FSMState.Retreat;
            }
        }
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
            case FSMState.Reload: UpdateReloadState(); break;
        }


        //Update the time
        elapsedTime += Time.deltaTime;

        //Go to dead state is no health left
        if (health <= 0)
        {
            curState = FSMState.Dead;
        } else if (health <= 50)
        {
            agent.speed = 75f;
        }
    }

    void UpdateReloadState()
    {
        if (enemy == null)
        {
            curState = FSMState.Scouting;
            return;
        }
        Debug.Log("Reload state");
        float dist = Vector3.Distance(transform.position, enemy.position);
        if (dist <= attackRange)
        {
            agent.isStopped = true;
            Quaternion targetRotation = Quaternion.LookRotation(enemy.position - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * curRotSpeed);

            //Go Forward
            transform.Translate(-Vector3.forward * Time.deltaTime * curSpeed);
        }

        else if (dist > attackRange)
        {
            agent.isStopped = false;
            curState = FSMState.Attack;
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
        if (enemy == null)
        {
            curState = FSMState.Scouting;
            return;
        }

        float dist = Vector3.Distance(transform.position, enemy.position);

        if (!hasEnemyPosition)
        {
            Vector3 newPos = RandomNavSphere(enemy.position, attackPointRadius, -1);
            agent.SetDestination(newPos);
            hasEnemyPosition = true;
        }

        Quaternion turretRotation = Quaternion.LookRotation(enemy.position - turret.position);
        turret.rotation = Quaternion.Slerp(turret.rotation, turretRotation, Time.deltaTime * turretRotationSpeed);

        if (dist <= attackRange)
        {
            //Shoot the bullets
            if (dist <= attackRange)
            {
                ShootBullet();
                curState = FSMState.Reload;
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
        //coordinator.regroupAll();
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
        //foreach (GameObject Ally in coordinator.alliedTanks)
        //{
        if (curState == FSMState.Scouting && other.tag != "Ally" && !other.GetComponent<Terrain>() && !other.CompareTag("Bullet"))
        {
            enemy = other.gameObject.transform;
            Debug.Log(enemy.name);
            //Switching to attack state
            Debug.Log("Go to regroup state");
            hasEnemyPosition = false;
            callRegroup();
            curState = FSMState.Regrouping;
        }
        //}
    }

    public void callRegroup()
    {
        if (enemy.position != null)
        {
            var pos = (transform.position - enemy.position)/ 2f;
            pos = transform.position + pos;
            Debug.Log(pos);
            int i = 0;
            ChangeStateOfAll(FSMState.Regrouping);
            foreach (GameObject tank in coordinator.alliedTanks)
            {
                if (!hasEnemyPosition)
                {
                    tank.GetComponent<NavMeshAgent>().SetDestination(pos + new Vector3(0,0,i));
                }
                i += 60;
            }
            hasEnemyPosition = true;
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

    void UpdateRegroupingState()
    {

        if (GetTotalDistanceTanks() < platoonDist)
        {
            ChangeStateOfAll(FSMState.Attack);
        }
    }

    void ChangeStateOfAll(FSMState changeState)
    {
        foreach (GameObject tank in coordinator.alliedTanks)
        {
            tank.GetComponent<SimpleFSMScouting>().curState = changeState;
        }
    }

    float GetTotalDistanceTanks()
    {
        float dist = 0f;
        foreach (GameObject tank in coordinator.alliedTanks)
        {
            foreach (GameObject tank2 in coordinator.alliedTanks)
            {
                dist += Vector3.Distance(tank.transform.position, tank2.transform.position);
            }
        }
        return dist;
    }
}
