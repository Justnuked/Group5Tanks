using UnityEngine;
using System.Collections;
using System.Linq;

public class NPCTankController : AdvancedFSM 
{
    public GameObject Bullet;
    private int currentHealth;
    private int maxHealth;
    private Transform hitTank;

    //Initialize the Finite state machine for the NPC tank
    protected override void Initialize()
    {
        maxHealth = 100;
        currentHealth = maxHealth;

        elapsedTime = 0.0f;
        shootRate = 2.0f;

        //Get the target enemy(Player)
        GameObject objPlayer = GameObject.FindGameObjectWithTag("Player");
        playerTransform = objPlayer.transform;

        if (!playerTransform)
            print("Player doesn't exist.. Please add one with Tag named 'Player'");

        //Get the turret of the tank
        turret = gameObject.transform.GetChild(0).transform;
        bulletSpawnPoint = turret.GetChild(0).transform;

        //Start Doing the Finite State Machine
        ConstructFSM();
    }

    //Update each frame
    protected override void FSMUpdate()
    {
        //Check for health
        elapsedTime += Time.deltaTime;
    }

    protected override void FSMFixedUpdate()
    {
        CurrentState.Reason(playerTransform, transform);
        CurrentState.Act(playerTransform, transform);
        CurrentState.ActWithoutPlayer(hitTank, transform);
    }

    public void SetTransition(Transition t) 
    { 
        PerformTransition(t); 
    }

    private void ConstructFSM()
    {
        //Get the list of points
        pointList = GameObject.FindGameObjectsWithTag("WandarPoint");

        Transform[] waypoints = new Transform[pointList.Length];
        int i = 0;
        foreach(GameObject obj in pointList)
        {
            waypoints[i] = obj.transform;
            i++;
        }

        PatrolState patrol = new PatrolState(waypoints);
        patrol.AddTransition(Transition.SawPlayer, FSMStateID.Chasing);
        patrol.AddTransition(Transition.NoHealth, FSMStateID.Dead);
        patrol.AddTransition(Transition.AboutToCollide, FSMStateID.Evading);
        patrol.AddTransition(Transition.LowHealth, FSMStateID.Fleeing);

        ChaseState chase = new ChaseState(waypoints);
        chase.AddTransition(Transition.LostPlayer, FSMStateID.Patrolling);
        chase.AddTransition(Transition.ReachPlayer, FSMStateID.Attacking);
        chase.AddTransition(Transition.NoHealth, FSMStateID.Dead);
        chase.AddTransition(Transition.LowHealth, FSMStateID.Fleeing);
        chase.AddTransition(Transition.AboutToCollide, FSMStateID.Evading);

        AttackState attack = new AttackState(waypoints);
        attack.AddTransition(Transition.LostPlayer, FSMStateID.Patrolling);
        attack.AddTransition(Transition.SawPlayer, FSMStateID.Chasing);
        attack.AddTransition(Transition.NoHealth, FSMStateID.Dead);
        attack.AddTransition(Transition.LowHealth, FSMStateID.Fleeing);
        attack.AddTransition(Transition.AboutToCollide, FSMStateID.Evading);

        DeadState dead = new DeadState();
        dead.AddTransition(Transition.NoHealth, FSMStateID.Dead);

        FleeState flee = new FleeState();
        flee.AddTransition(Transition.LostPlayer, FSMStateID.Patrolling);
        flee.AddTransition(Transition.ReachPlayer, FSMStateID.Attacking);
        flee.AddTransition(Transition.AboutToCollide, FSMStateID.Evading);
        flee.AddTransition(Transition.NoHealth, FSMStateID.Dead);

        EvadeState evade = new EvadeState();
        evade.AddTransition(Transition.LostPlayer, FSMStateID.Patrolling);
        evade.AddTransition(Transition.SawPlayer, FSMStateID.Chasing);
        evade.AddTransition(Transition.ReachPlayer, FSMStateID.Attacking);
        evade.AddTransition(Transition.NoHealth, FSMStateID.Dead);
        evade.AddTransition(Transition.AboutToCollide, FSMStateID.Evading);
        evade.AddTransition(Transition.LowHealth, FSMStateID.Fleeing);

        AddFSMState(patrol);
        AddFSMState(chase);
        AddFSMState(attack);
        AddFSMState(dead);
        AddFSMState(flee);
        AddFSMState(evade);
    }

    /// <summary>
    /// Check the collision with the bullet
    /// </summary>
    /// <param name="collision"></param>
    void OnCollisionEnter(Collision collision)
    {
        //Reduce health
        if (collision.gameObject.tag == "Bullet")
        {
            currentHealth -= 25;

            if (currentHealth <= 0)
            {
                Debug.Log("Switch to Dead State");
                SetTransition(Transition.NoHealth);
                Explode();
            }

            Debug.Log("Switch to Flee State");
            SetTransition(Transition.LowHealth);
        }
    }

    protected void Explode()
    {
        float rndX = Random.Range(10.0f, 30.0f);
        float rndZ = Random.Range(10.0f, 30.0f);
        for (int i = 0; i < 3; i++)
        {
            GetComponent<Rigidbody>().AddExplosionForce(10000.0f, transform.position - new Vector3(rndX, 10.0f, rndZ), 40.0f, 10.0f);
            GetComponent<Rigidbody>().velocity = transform.TransformDirection(new Vector3(rndX, 20.0f, rndZ));
        }

        Destroy(gameObject, 1.5f);
    }

    /// <summary>
    /// Shoot the bullet from the turret
    /// </summary>
    public void ShootBullet()
    {
        if (elapsedTime >= shootRate)
        {
            Instantiate(Bullet, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
            elapsedTime = 0.0f;
        }
    }

    public int GetMaxHealth()
    {
        return maxHealth;
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log(other.transform.tag);
        if (other.transform.tag == "EnemyTank")
        {
            hitTank = other.transform;
            SetTransition(Transition.AboutToCollide);
        }

    }
}
