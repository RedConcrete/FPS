using System;
using UnityEngine;

public class BulletScript : MonoBehaviour
{
    [Header("Setting")]
    public float lifeTime = 10;

    private void Awake()
    {
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        
        if (gameObject.tag != "Arrow")
        {
            Debug.Log("Tag: " + other.gameObject.tag);
            if (other.gameObject.tag == "Enemy") // Check if the hit object is an enemy
            {

                Destroy(other.gameObject); // Destroy the enemy object
                Destroy(gameObject); // Destroy the bullet object

            }
        }
    }
}
