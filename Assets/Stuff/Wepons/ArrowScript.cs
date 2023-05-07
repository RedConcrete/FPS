using System;
using UnityEngine;

public class ArrowScript : MonoBehaviour
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
        if (other.gameObject.tag == "Player") // Check if the hit object is an enemy
        {
            Debug.Log("OTHER Tag: " + other.gameObject.tag);
            Debug.Log("OWN Tag: " + gameObject.tag);

            HP playerHP = gameObject.GetComponent<HP>();

            playerHP.hPPlayer -= DMG; 
            
            Destroy(gameObject); // Destroy the bullet object

        }
    }
}
