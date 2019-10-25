using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Die : MonoBehaviour
{
    private float HP;
    // Start is called before the first frame update
    void Start()
    {
        HP = 200;
    }

    // Update is called once per frame
    void Update()
    {
        if (HP <= 0)
            Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Bullet")
        {
            HP -= 25f;
        }
    }
}
