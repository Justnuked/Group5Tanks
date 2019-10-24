using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankCoordinator : MonoBehaviour
{
    //Array for all allied tanks
    public GameObject[] alliedTanks;

    //Array for enemy tanks
    public GameObject[] enemyTanks;

    // Start is called before the first frame update
    void Start()
    {
        alliedTanks = GameObject.FindGameObjectsWithTag("Ally");

        foreach (var ally in alliedTanks)
        {
            Debug.Log(ally);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
