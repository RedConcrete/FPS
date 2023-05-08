using System;
using UnityEngine;

public class BulletScript : MonoBehaviour
{
    [Header("Setting")]
    public float lifeTime;
    public int DMG;

    private void Awake()
    {
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        /*
        if (other.gameObject.tag == "Enemy") // Check if the hit object is an enemy
        {
            Debug.Log("OTHER Tag: " + other.gameObject.tag);
            Debug.Log("OWN Tag: " + gameObject.tag);

            Destroy(other.gameObject); // Destroy the enemy object
            Destroy(gameObject); // Destroy the bullet object

        }
        */
    }
}
